using ClosedXML.Excel;
using KpiEvaluation.Application;
using KpiEvaluation.Application.Repositories;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class SpreadsheetRepository(KpiDbContext db, IEmployeeRepository employees, IKpiRepository kpis,
    IEmployeeLeaveRepository leaves, IEvaluationRepository evaluations, ILogger<SpreadsheetRepository> logger) : ISpreadsheetRepository
{
    public async Task<SpreadsheetFileDto> ExportAsync(SpreadsheetResource resource, SpreadsheetExportInput input, int userId,
        CancellationToken ct = default)
    {
        using var workbook = SpreadsheetWorkbook.Create(resource);
        var sheet = workbook.Worksheet(1);
        var row = 2;
        var selectedIds = input.Ids?.ToHashSet();
        bool Selected(long id) => selectedIds is null || selectedIds.Contains(id);
        void Write(params object?[] values)
        {
            ct.ThrowIfCancellationRequested();
            if (row > SpreadsheetWorkbook.MaxRows + 1) throw new ValidationException("Dışa aktarma en fazla 10.000 satır olabilir. Filtreleri daraltın.");
            SpreadsheetWorkbook.WriteRow(sheet, row++, values);
        }

        if (!input.Template)
        {
            switch (resource)
            {
                case SpreadsheetResource.Employees:
                    foreach (var item in (await employees.GetAllAsync(false, ct)).Where(x => Selected(x.Id)))
                        Write(item.Id, item.FullName, item.ProfessionId, item.ProfessionName, item.IsActive);
                    break;
                case SpreadsheetResource.Kpis:
                    foreach (var item in (await kpis.GetAllAsync(ct)).Where(x => Selected(x.Id)))
                    {
                        var scopes = item.Professions?.Count > 0 ? item.Professions
                            : item.ProfessionId is { } professionId ? new[] { new LookupDto(professionId, item.ProfessionName ?? "") } : [];
                        Write(item.Id, item.Title, item.Description, string.Join(';', scopes.Select(x => x.Id)),
                            string.Join("; ", scopes.Select(x => x.Name)), item.IsCommon, item.DisplayOrder, item.IsActive, item.Category,
                            item.CompanyGoalId, item.CompanyGoalName, (int)item.MeasurementType, item.Unit, item.TargetValue,
                            (int)item.TargetDirection, item.WeightPercent, item.DataSource, (int)item.ReviewFrequency, item.ValidFrom,
                            item.ValidTo, item.IsSpecific, item.IsMeasurable, item.IsAchievable, item.IsRelevant, item.IsTimeBound);
                    }
                    break;
                case SpreadsheetResource.Leaves:
                    var page = 1;
                    EmployeeLeavePageDto data;
                    do
                    {
                        data = await leaves.GetAsync(input.EmployeeId, input.ProfessionId, input.From, input.To, input.ActiveOnly, page, 100, ct);
                        if (data.TotalCount > SpreadsheetWorkbook.MaxRows) throw new ValidationException("Dışa aktarma en fazla 10.000 kayıt olabilir. Filtreleri daraltın.");
                        foreach (var item in data.Items)
                            Write(item.Id, item.EmployeeId, item.EmployeeName, item.ProfessionName, item.StartDate, item.EndDate, item.LeaveType, item.Note, item.IsActive);
                        page++;
                    } while ((page - 1) * 100 < data.TotalCount);
                    break;
                case SpreadsheetResource.Evaluations:
                    var history = (await evaluations.GetHistoryAsync(null, null, null, ct, userId)).Where(x => Selected(x.Id)).ToArray();
                    var ids = history.Select(x => x.Id).ToArray();
                    var sources = await db.EmployeeEvaluations.AsNoTracking().Where(x => ids.Contains(x.Id))
                        .Select(x => new { x.Id, x.EmployeeId, x.EvaluationPeriodId,
                            Items = x.Details.OrderBy(d => d.IsCommonSnapshot).ThenBy(d => d.DisplayOrderSnapshot).ThenBy(d => d.Id)
                                .Select(d => new { d.KpiCriterionId, d.KpiTitleSnapshot, d.Score, d.ActualValue, d.Note }).ToList() })
                        .ToDictionaryAsync(x => x.Id, ct);
                    foreach (var item in history)
                    {
                        var source = sources[item.Id];
                        foreach (var detail in source.Items)
                            Write($"D{item.Id}", source.EmployeeId, item.EmployeeName, item.ProfessionName, source.EvaluationPeriodId,
                                item.PeriodName, item.EvaluationDate, item.GeneralNote, detail.KpiCriterionId, KpiQuestionText.CapitalizeWords(detail.KpiTitleSnapshot),
                                detail.Score, detail.ActualValue, detail.Note, item.StatusName, item.ManagerFeedback, item.EmployeeFeedback,
                                item.DevelopmentAction, item.FollowUpDate, item.EvaluatorUserId, item.EvaluatorName);
                    }
                    break;
            }
        }
        if (input.Template) await AddLookupsAsync(workbook, resource, ct);
        if (row > 2) sheet.Range(1, 1, row - 1, SpreadsheetWorkbook.Headers(resource).Length).SetAutoFilter();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new(stream.ToArray(), $"{resource.ToString().ToLowerInvariant()}-{(input.Template ? "sablon" : "excel")}.xlsx");
    }

    public async Task<ImportResultDto> ImportAsync(SpreadsheetResource resource, Stream content, string fileName,
        bool permanentEvaluations, int userId, CancellationToken ct = default)
    {
        var rows = await SpreadsheetWorkbook.ReadAsync(resource, content, fileName, ct);
        var groups = rows.GroupBy(x => resource == SpreadsheetResource.Evaluations ? x.Values["Aktarım Grubu"]
            : long.TryParse(x.Values["ID"], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var id)
                ? $"ID:{id}" : $"Satır:{x.Number}");
        var imported = 0;
        var skipped = 0;
        var errors = new List<string>();
        foreach (var group in groups)
        {
            ct.ThrowIfCancellationRequested();
            var first = group.First();
            try
            {
                if (resource == SpreadsheetResource.Evaluations)
                {
                    if (await ImportEvaluationAsync(group.ToArray(), permanentEvaluations, userId, ct)) imported++;
                    else skipped++;
                }
                else
                {
                    if (group.Count() > 1) throw new ValidationException("Aynı ID bir dosyada tekrar edemez.");
                    await using var transaction = await db.Database.BeginTransactionAsync(ct);
                    await ImportRowAsync(resource, first, userId, ct);
                    await transaction.CommitAsync(ct);
                    imported++;
                }
            }
            catch (DuplicateEvaluationException) { skipped++; }
            catch (Exception ex) when (ex is ValidationException or AuthorizationException)
            {
                errors.Add($"Satır {first.Number}: {ex.Message}");
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Excel aktarımında {Resource} satır {Row} kaydedilemedi.", resource, first.Number);
                errors.Add($"Satır {first.Number}: Kayıt kaydedilemedi. Alan uzunluklarını, sayı aralıklarını ve ilişkili kayıtları kontrol edin.");
            }
            finally
            {
                // A failed row must never be persisted by a later row's SaveChanges.
                db.ChangeTracker.Clear();
            }
        }
        return new(imported, skipped, errors);
    }

    private async Task ImportRowAsync(SpreadsheetResource resource, SpreadsheetRow row, int userId, CancellationToken ct)
    {
        var id = row.Id();
        if (resource != SpreadsheetResource.Leaves && id > int.MaxValue) throw new ValidationException("ID geçerli aralığın dışında.");
        switch (resource)
        {
            case SpreadsheetResource.Employees:
                await employees.SaveAsync((int?)id, row.Text("Ad Soyad", 200, true), row.Int("Meslek ID"), row.Bool("Aktif", true), ct);
                break;
            case SpreadsheetResource.Leaves:
                await leaves.SaveAsync(new(id, row.Int("Çalışan ID"), row.Day("Başlangıç") ?? default, row.Day("Bitiş") ?? default,
                    row.Text("İzin Türü", 100, true), row.Text("Açıklama"), row.Bool("Aktif", true)), userId, ct);
                break;
            case SpreadsheetResource.Kpis:
                var common = row.Bool("Herkes");
                var professions = new List<int>();
                if (!common && row.Text("Ekip ID'leri") is { Length: > 0 } scope)
                    foreach (var value in scope.Split(';', StringSplitOptions.TrimEntries))
                    {
                        if (!int.TryParse(value, out var professionId) || professionId <= 0) throw new ValidationException("Ekip ID'leri noktalı virgülle ayrılmış pozitif sayılar olmalıdır.");
                        professions.Add(professionId);
                    }
                await kpis.SaveAsync((int?)id, row.Text("KPI", 1000, true), row.Text("Açıklama"), null, common, row.Int("Sıra", 1),
                    row.Bool("Aktif", true), ct, row.Text("Kategori", 150), row.OptionalInt("Şirket Hedefi ID"),
                    row.Enum("Ölçüm Türü", KpiMeasurementType.ManualScore), row.Text("Birim", 50), row.Decimal("Hedef Değer"),
                    row.Enum("Hedef Yönü", KpiTargetDirection.AtLeast), row.Decimal("Ağırlık") ?? 0, row.Text("Veri Kaynağı", 500),
                    row.Enum("Gözden Geçirme", KpiReviewFrequency.SemiAnnual), row.Day("Geçerlilik Başlangıcı"), row.Day("Geçerlilik Bitişi"),
                    row.Bool("Belirli"), row.Bool("Ölçülebilir"), row.Bool("Ulaşılabilir"), row.Bool("İlgili"), row.Bool("Zaman Sınırlı"), professions);
                break;
            default: throw new ValidationException("Geçersiz Excel sayfası.");
        }
    }

    private async Task<bool> ImportEvaluationAsync(IReadOnlyList<SpreadsheetRow> rows, bool permanent, int userId, CancellationToken ct)
    {
        var first = rows[0];
        first.Text("Aktarım Grubu", 100, true);
        string[] metadata = ["Çalışan ID", "Dönem ID", "Değerlendirme Tarihi", "Genel Not", "Amir Görüşü", "Çalışan Görüşü", "Gelişim Aksiyonu", "Takip Tarihi"];
        if (rows.Any(row => metadata.Any(column => row.Values[column] != first.Values[column])))
            throw new ValidationException("Aynı aktarım grubundaki çalışan, dönem, tarih ve genel açıklamalar eşleşmelidir.");
        var employeeId = first.Int("Çalışan ID");
        var periodId = first.Int("Dönem ID");
        var input = new EvaluationSaveInput(employeeId, periodId, first.Text("Genel Not"),
            rows.Select(row => new EvaluationItemInput(row.Int("KPI ID"), row.Int("Puan", 0), row.Text("Madde Notu"), row.Decimal("Gerçekleşen"))).ToArray(),
            first.Date("Değerlendirme Tarihi"), permanent ? EvaluationStatus.Approved : EvaluationStatus.Draft,
            first.Text("Amir Görüşü"), first.Text("Çalışan Görüşü"), first.Text("Gelişim Aksiyonu"), first.Day("Takip Tarihi"));
        if ((await evaluations.GetHistoryAsync(employeeId, null, periodId, ct, userId, userId)).Count > 0) return false;
        await evaluations.SaveAsync(input, ct, userId);
        return true;
    }

    private async Task AddLookupsAsync(XLWorkbook workbook, SpreadsheetResource resource, CancellationToken ct)
    {
        void Add(string name, string[] headers, IEnumerable<object?[]> rows)
        {
            var sheet = workbook.AddWorksheet(name);
            SpreadsheetWorkbook.WriteRow(sheet, 1, headers);
            sheet.Row(1).Style.Font.Bold = true;
            sheet.Columns(1, headers.Length).Width = 28;
            var index = 2;
            foreach (var values in rows) SpreadsheetWorkbook.WriteRow(sheet, index++, values);
        }
        if (resource is SpreadsheetResource.Employees or SpreadsheetResource.Kpis)
        {
            var professions = await db.Professions.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
                .Select(x => new LookupDto(x.Id, x.Name)).ToListAsync(ct);
            Add("Ekip Referansları", ["ID", "Ekip"], professions.Select(x => new object?[] { x.Id, x.Name }));
        }
        if (resource == SpreadsheetResource.Kpis)
        {
            var goals = await db.CompanyGoals.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
                .Select(x => new LookupDto(x.Id, x.Name)).ToListAsync(ct);
            Add("Hedef Referansları", ["ID", "Şirket Hedefi"], goals.Select(x => new object?[] { x.Id, x.Name }));
        }
        if (resource is SpreadsheetResource.Leaves or SpreadsheetResource.Evaluations)
        {
            var activeEmployees = await employees.GetAllAsync(true, ct);
            Add("Çalışan Referansları", ["ID", "Çalışan", "Ekip"], activeEmployees.Select(x => new object?[] { x.Id, x.FullName, x.ProfessionName }));
        }
        if (resource == SpreadsheetResource.Evaluations)
        {
            var periods = await db.EvaluationPeriods.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.StartDate)
                .Select(x => new { x.Id, x.Name, x.StartDate, x.EndDate }).ToListAsync(ct);
            Add("Dönem Referansları", ["ID", "Dönem", "Başlangıç", "Bitiş"], periods.Select(x => new object?[] { x.Id, x.Name, x.StartDate, x.EndDate }));
            var criteria = (await kpis.GetAllAsync(ct)).Where(x => x.IsActive);
            Add("KPI Referansları", ["ID", "KPI", "Herkes", "Ekip ID'leri", "Ölçüm Türü", "Hedef", "Hedef Yönü", "Geçerlilik Başlangıcı", "Geçerlilik Bitişi"],
                criteria.Select(x => new object?[] { x.Id, x.Title, x.IsCommon,
                    string.Join(';', x.Professions?.Count > 0 ? x.Professions.Select(p => p.Id) : (x.ProfessionId is { } p ? new[] { p } : [])),
                    (int)x.MeasurementType, x.TargetValue, (int)x.TargetDirection, x.ValidFrom, x.ValidTo }));
        }
    }
}
