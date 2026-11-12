using ClosedXML.Excel;
using System.Globalization;
using System.Text.Json;
using KpiEvaluation.Application;
using KpiEvaluation.Application.Repositories;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class SpreadsheetRepository(KpiDbContext db, IEmployeeRepository employees, IKpiRepository kpis,
    IEmployeeLeaveRepository leaves, IEvaluationRepository evaluations, ILogger<SpreadsheetRepository> logger,
    IProfessionRepository professionRepository) : ISpreadsheetRepository
{
    private const string UnassignedProfessionName = "Meslek atanmadı";

    public async Task<SpreadsheetFileDto> ExportAsync(SpreadsheetResource resource, SpreadsheetExportInput input, int userId,
        CancellationToken ct = default)
    {
        if (resource == SpreadsheetResource.Calibration && input.PeriodIds?.Any(x => x <= 0) == true)
            throw new ValidationException("Geçerli dönemler seçmelisiniz.");
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
                        Write(item.FullName, item.ProfessionName, item.IsActive);
                    break;
                case SpreadsheetResource.Kpis:
                    foreach (var item in (await kpis.GetAllAsync(ct)).Where(x => Selected(x.Id)))
                    {
                        var scopes = item.Professions?.Count > 0 ? item.Professions
                            : item.ProfessionId is { } professionId ? new[] { new LookupDto(professionId, item.ProfessionName ?? "") } : [];
                        Write(item.Title, item.Description, string.Join("; ", scopes.Select(x => x.Name)),
                            item.IsCommon, item.Category, item.CompanyGoalName);
                    }
                    break;
                case SpreadsheetResource.Leaves:
                    var page = 1;
                    EmployeeLeaveBalancePageDto data;
                    do
                    {
                        data = await leaves.GetBalancesAsync(input.EmployeeId, input.ProfessionId, input.From, input.To, input.ActiveOnly, page, 100, ct);
                        if (data.TotalCount > SpreadsheetWorkbook.MaxRows) throw new ValidationException("Dışa aktarma en fazla 10.000 kayıt olabilir. Filtreleri daraltın.");
                        foreach (var item in data.Items)
                            Write(item.EmployeeName, item.RemainingDays, item.EntitlementDate);
                        page++;
                    } while ((page - 1) * 100 < data.TotalCount);
                    break;
                case SpreadsheetResource.Evaluations:
                    var history = (await evaluations.GetHistoryAsync(null, null, null, ct, userId)).Where(x => Selected(x.Id)).ToArray();
                    var ids = history.Select(x => x.Id).ToArray();
                    var sources = await db.EmployeeEvaluations.AsNoTracking().Where(x => ids.Contains(x.Id))
                        .Select(x => new { x.Id,
                            Items = x.Details.OrderBy(d => d.IsCommonSnapshot).ThenBy(d => d.DisplayOrderSnapshot).ThenBy(d => d.Id)
                                .Select(d => new { d.KpiTitleSnapshot, d.Score, d.Note }).ToList() })
                        .ToDictionaryAsync(x => x.Id, ct);
                    foreach (var item in history)
                    {
                        var source = sources[item.Id];
                        foreach (var detail in source.Items)
                            Write(item.EmployeeName, item.ProfessionName, item.PeriodName, detail.Note,
                                KpiQuestionText.CapitalizeWords(detail.KpiTitleSnapshot), detail.Score, item.ManagerFeedback, item.EvaluatorName);
                    }
                    break;
                case SpreadsheetResource.Calibration:
                    var periods = input.PeriodIds?.ToHashSet();
                    var approved = ApprovedManagerResults.Create(await evaluations.GetHistoryAsync(null, null,
                        periods?.Count == 1 ? periods.Single() : null, ct, userId));
                    var culture = CultureInfo.GetCultureInfo("tr-TR");
                    foreach (var item in approved.Where(x => periods is not { Count: > 0 } || periods.Contains(x.PeriodId)))
                    {
                        Write(item.EmployeeName, item.ProfessionName, item.PeriodName,
                            string.Join("\n", item.Scores.Select(x => $"{x.EvaluatorName}: {x.Percentage.ToString("0.##", culture)} / 100"
                                + (x.WeightPercent is { } weight ? $" · %{weight.ToString("0.##", culture)} etki" : " · Güncel ağırlıklandırmaya dahil değil"))),
                            item.CombinedPercentage);
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
        if (resource == SpreadsheetResource.Calibration)
            throw new ValidationException("Onaylı Amir Sonuçları raporu içe aktarılamaz.");
        var rows = await SpreadsheetWorkbook.ReadAsync(resource, content, fileName, ct);
        var groups = rows.GroupBy(x => resource == SpreadsheetResource.Evaluations ? EvaluationGroupKey(x)
            : resource == SpreadsheetResource.Employees && !x.Values.ContainsKey("Meslek ID")
                ? x.Values["Ad Soyad"].Normalize().ToUpper(CultureInfo.GetCultureInfo("tr-TR"))
            : resource == SpreadsheetResource.Leaves && !x.Values.ContainsKey("Çalışan ID")
                ? x.Values["Adı Soyadı"].Normalize().ToUpper(CultureInfo.GetCultureInfo("tr-TR"))
            : resource == SpreadsheetResource.Kpis && !x.Values.ContainsKey("Ekip ID'leri")
                ? KpiGroupKey(x)
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
                    if (group.Count() > 1) throw new ValidationException(resource == SpreadsheetResource.Employees && !first.Values.ContainsKey("Meslek ID")
                        ? "Aynı Ad Soyad bir dosyada tekrar edemez."
                        : resource == SpreadsheetResource.Leaves && !first.Values.ContainsKey("Çalışan ID")
                            ? "Aynı Adı Soyadı bir dosyada tekrar edemez."
                            : resource == SpreadsheetResource.Kpis && !first.Values.ContainsKey("Ekip ID'leri")
                                ? "Aynı KPI ve ekip kapsamı bir dosyada tekrar edemez." : "Aynı ID bir dosyada tekrar edemez.");
                    await using var transaction = resource == SpreadsheetResource.Employees
                        || (resource == SpreadsheetResource.Leaves && !first.Values.ContainsKey("Çalışan ID"))
                        || (resource == SpreadsheetResource.Kpis && !first.Values.ContainsKey("Ekip ID'leri"))
                        ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct)
                        : await db.Database.BeginTransactionAsync(ct);
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
        if (resource == SpreadsheetResource.Employees && !row.Values.ContainsKey("Meslek ID"))
        {
            await ImportEmployeeAsync(row, ct);
            return;
        }
        if (resource == SpreadsheetResource.Kpis && !row.Values.ContainsKey("Ekip ID'leri"))
        {
            await ImportKpiAsync(row, ct);
            return;
        }
        if (resource == SpreadsheetResource.Leaves && !row.Values.ContainsKey("Çalışan ID"))
        {
            var name = row.Text("Adı Soyadı", 200, true);
            var remainingDays = row.Decimal("Kalan İzin Gün Sayısı")
                ?? throw new ValidationException("Kalan İzin Gün Sayısı zorunludur.");
            var entitlementDate = row.Day("İzin Hak Ediş Tarihi")
                ?? throw new ValidationException("İzin Hak Ediş Tarihi zorunludur.");
            var matches = await db.Employees.AsNoTracking().Where(x => x.IsActive && x.FullName == name)
                .Select(x => x.Id).Take(2).ToListAsync(ct);
            if (matches.Count > 1) throw new ValidationException("Adı Soyadı birden fazla aktif çalışanla eşleşiyor; izin kaydı eşleştirilemedi.");
            int employeeId;
            if (matches.Count == 1) employeeId = matches[0];
            else
            {
                if (await db.Employees.AnyAsync(x => x.FullName == name, ct))
                    throw new ValidationException("Bu adla pasif bir çalışan var. İzin yüklemeden önce mevcut çalışanı etkinleştirin.");
                var professionId = await ResolveImportProfessionAsync(UnassignedProfessionName, ct);
                employeeId = (await employees.SaveAsync(null, name, professionId, true, ct)).Id;
            }
            await leaves.SaveBalanceAsync(new(employeeId, remainingDays, entitlementDate), userId, ct);
            return;
        }
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

    private async Task ImportKpiAsync(SpreadsheetRow row, CancellationToken ct)
    {
        var title = KpiQuestionText.CapitalizeWords(row.Text("KPI", 1000, true));
        var description = row.Text("Açıklama");
        var category = row.Text("Kategori", 150);
        var common = row.Bool("Herkes");
        var professionIds = new HashSet<int>();
        if (!common)
        {
            foreach (var name in row.Text("Ekipler").Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var teams = await db.Professions.AsNoTracking().Where(x => x.IsActive && x.Name == name)
                    .Select(x => x.Id).Take(2).ToListAsync(ct);
                if (teams.Count != 1) throw new ValidationException("Her ekip adı tek bir aktif ekiple eşleşmelidir. Ekip Referansları sayfasındaki adları kullanın.");
                professionIds.Add(teams[0]);
            }
            if (professionIds.Count == 0) throw new ValidationException("Herkes=Hayır olduğunda en az bir ekip adı belirtmelisiniz.");
        }
        var goalName = row.Text("Bağlı olduğu şirket Hedefi", 250);
        int? goalId = null;
        if (goalName.Length > 0)
        {
            var goals = await db.CompanyGoals.AsNoTracking().Where(x => x.IsActive && x.Name == goalName)
                .Select(x => x.Id).Take(2).ToListAsync(ct);
            if (goals.Count != 1) throw new ValidationException("Şirket hedefi adı tek bir aktif hedefle eşleşmelidir. Hedef Referansları sayfasındaki adları kullanın.");
            goalId = goals[0];
        }
        var candidates = await db.KpiCriteria.AsNoTracking().Where(x => x.IsCommon == common
            && (common || (x.ProfessionId != null && professionIds.Contains(x.ProfessionId.Value))
                || x.Professions.Any(p => professionIds.Contains(p.ProfessionId))))
            .Select(x => new { x.Id, x.Title, x.ProfessionId, ScopeIds = x.Professions.Select(p => p.ProfessionId).ToList() })
            .ToListAsync(ct);
        var comparer = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), true);
        var matches = candidates.Where(x => comparer.Equals(KpiQuestionText.CapitalizeWords(x.Title).Normalize(), title.Normalize())
            && (common || professionIds.SetEquals(x.ScopeIds.Concat(x.ProfessionId is { } primary ? new[] { primary } : []))))
            .Take(2).ToArray();
        if (matches.Length > 1) throw new ValidationException("Aynı başlık ve ekip kapsamında birden fazla KPI var; kayıt eşleştirilemedi.");
        var existing = matches.Length == 0 ? null : await db.KpiCriteria.AsNoTracking().SingleAsync(x => x.Id == matches[0].Id, ct);
        await kpis.SaveAsync(existing?.Id, title, description, null, common, existing?.DisplayOrder ?? 1, existing?.IsActive ?? true, ct,
            category, goalId, existing?.MeasurementType ?? KpiMeasurementType.ManualScore, existing?.Unit, existing?.TargetValue,
            existing?.TargetDirection ?? KpiTargetDirection.AtLeast, existing?.WeightPercent ?? 0, existing?.DataSource,
            existing?.ReviewFrequency ?? KpiReviewFrequency.SemiAnnual, existing?.ValidFrom, existing?.ValidTo,
            existing?.IsSpecific ?? false, existing?.IsMeasurable ?? false, existing?.IsAchievable ?? false,
            existing?.IsRelevant ?? false, existing?.IsTimeBound ?? false, professionIds.Order().ToArray());
    }

    private static string KpiGroupKey(SpreadsheetRow row)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var common = row.Values["Herkes"].ToLowerInvariant() is "evet" or "true" or "1";
        var teams = common ? [] : row.Values["Ekipler"].Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Normalize().ToUpper(culture)).Distinct().Order().ToArray();
        return JsonSerializer.Serialize(new { Title = row.Values["KPI"].Normalize().ToUpper(culture), Common = common, Teams = teams });
    }

    private async Task ImportEmployeeAsync(SpreadsheetRow row, CancellationToken ct)
    {
        var fullName = row.Text("Ad Soyad", 200, true);
        var professionName = row.Text("Meslek", 100);
        var isActive = row.Bool("Aktiflik durumu", true);
        var matches = await db.Employees.AsNoTracking().Where(x => x.FullName == fullName)
            .Select(x => new { x.Id, x.ProfessionId }).Take(2).ToListAsync(ct);
        int? professionId = professionName.Length == 0 ? null : await ResolveImportProfessionAsync(professionName, ct);
        int? employeeId;
        if (matches.Count > 1)
        {
            var scoped = await db.Employees.AsNoTracking().Where(x => x.FullName == fullName && x.ProfessionId == professionId)
                .Select(x => (int?)x.Id).Take(2).ToListAsync(ct);
            if (scoped.Count != 1) throw new ValidationException("Aynı adla birden fazla çalışan var; meslek bilgisiyle tek bir kayıt eşleştirilemedi.");
            employeeId = scoped[0];
        }
        else employeeId = matches.SingleOrDefault()?.Id;
        professionId ??= matches.SingleOrDefault()?.ProfessionId ?? await ResolveImportProfessionAsync(UnassignedProfessionName, ct);
        await employees.SaveAsync(employeeId, fullName, professionId.Value, isActive, ct);
    }

    private async Task<int> ResolveImportProfessionAsync(string name, CancellationToken ct)
    {
        var matches = await db.Professions.AsNoTracking().Where(x => x.Name == name)
            .Select(x => new { x.Id, x.IsActive }).Take(2).ToListAsync(ct);
        if (matches.Count > 1) throw new ValidationException("Meslek adı birden fazla kayıtla eşleşiyor.");
        if (matches.Count == 1)
        {
            if (!matches[0].IsActive) throw new ValidationException("Bu meslek pasif durumda. Önce mevcut mesleği etkinleştirin.");
            return matches[0].Id;
        }
        return (await professionRepository.SaveAsync(null, name, true, ct)).Id;
    }

    private async Task<bool> ImportEvaluationAsync(IReadOnlyList<SpreadsheetRow> rows, bool permanent, int userId, CancellationToken ct)
    {
        if (rows[0].Values.ContainsKey("Aktarım Grubu"))
            return await ImportLegacyEvaluationAsync(rows, permanent, userId, ct);
        var first = rows[0];
        var employeeName = first.Text("Çalışan", 200, true);
        var teamName = first.Text("Ekip", 100, true);
        var periodName = first.Text("Dönem", 100, true);
        var feedback = first.Text("Amir Görüşü");
        if (rows.Any(row => row.Text("Amir Görüşü") != feedback))
            throw new ValidationException("Aynı değerlendirmenin tüm satırlarında Amir Görüşü aynı olmalıdır.");
        var matches = await db.Employees.AsNoTracking()
            .Where(x => x.IsActive && x.FullName == employeeName && x.Profession.Name == teamName)
            .Select(x => new { x.Id, x.ProfessionId }).Take(2).ToListAsync(ct);
        if (matches.Count != 1)
            throw new ValidationException(matches.Count == 0 ? "Çalışan ve ekip eşleşmedi. Yardımcı sayfadaki adları kullanın."
                : "Aynı ad ve ekipte birden fazla çalışan var; değerlendirme eşleştirilemedi.");
        var employee = matches[0];
        var periods = await db.EvaluationPeriods.AsNoTracking().Where(x => x.IsActive && x.Name == periodName).Take(2).ToListAsync(ct);
        if (periods.Count != 1) throw new ValidationException("Dönem adı tek bir aktif dönemle eşleşmelidir.");
        var period = periods[0];
        if ((await evaluations.GetHistoryAsync(employee.Id, null, period.Id, ct, userId, userId)).Count > 0) return false;
        var criteria = await db.KpiCriteria.AsNoTracking()
            .Where(x => x.IsActive && (x.IsCommon || x.ProfessionId == employee.ProfessionId || x.Professions.Any(p => p.ProfessionId == employee.ProfessionId))
                && (x.ValidFrom == null || x.ValidFrom <= period.EndDate) && (x.ValidTo == null || x.ValidTo >= period.StartDate))
            .Select(x => new { x.Id, x.Title, x.MeasurementType }).ToListAsync(ct);
        var comparer = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), true);
        var items = new List<EvaluationItemInput>();
        foreach (var row in rows)
        {
            var title = row.Text("KPI", 1000, true);
            var candidates = criteria.Where(x => comparer.Equals(x.Title.Trim().Normalize(), title.Normalize())).Take(2).ToArray();
            if (candidates.Length != 1)
                throw new ValidationException($"Satır {row.Number}: KPI adı tek bir uygun soruyla eşleşmelidir: {title}");
            var criterion = candidates[0];
            var score = row.Int("Puan", 0);
            if (criterion.MeasurementType != KpiMeasurementType.ManualScore && (permanent || score != 0))
                throw new ValidationException($"Satır {row.Number}: Sayısal KPI gerçekleşen değer gerektirir; bu değerlendirmeyi uygulamadan tamamlayın.");
            items.Add(new EvaluationItemInput(criterion.Id, score, row.Text("Not")));
        }
        await evaluations.SaveAsync(new EvaluationSaveInput(employee.Id, period.Id, null, items,
            Status:permanent ? EvaluationStatus.Approved : EvaluationStatus.Draft, ManagerFeedback:feedback), ct, userId);
        return true;
    }

    private static string EvaluationGroupKey(SpreadsheetRow row)
    {
        if (row.Values.TryGetValue("Aktarım Grubu", out var legacyGroup)) return $"legacy:{legacyGroup}";
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        return JsonSerializer.Serialize(new[] { row.Values["Çalışan"], row.Values["Ekip"], row.Values["Dönem"],
            row.Values.GetValueOrDefault("Değerlendiren Amir") ?? "" }.Select(value => value.Normalize().ToUpper(culture)));
    }

    private async Task<bool> ImportLegacyEvaluationAsync(IReadOnlyList<SpreadsheetRow> rows, bool permanent, int userId, CancellationToken ct)
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
            if (resource == SpreadsheetResource.Employees)
                Add("Meslek Referansları", ["Meslek"], professions.Select(x => new object?[] { x.Name }));
            else Add("Ekip Referansları", ["Ekip"], professions.Select(x => new object?[] { x.Name }));
        }
        if (resource == SpreadsheetResource.Kpis)
        {
            var goals = await db.CompanyGoals.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
                .Select(x => new LookupDto(x.Id, x.Name)).ToListAsync(ct);
            Add("Hedef Referansları", ["Bağlı olduğu şirket Hedefi"], goals.Select(x => new object?[] { x.Name }));
        }
        if (resource is SpreadsheetResource.Leaves or SpreadsheetResource.Evaluations)
        {
            var activeEmployees = await employees.GetAllAsync(true, ct);
            if (resource == SpreadsheetResource.Leaves)
                Add("Çalışan Referansları", ["Adı Soyadı"], activeEmployees.Select(x => new object?[] { x.FullName }));
            else if (resource == SpreadsheetResource.Evaluations)
                Add("Çalışan Referansları", ["Çalışan", "Ekip"], activeEmployees.Select(x => new object?[] { x.FullName, x.ProfessionName }));
        }
        if (resource == SpreadsheetResource.Evaluations)
        {
            var periods = await db.EvaluationPeriods.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.StartDate)
                .Select(x => new { x.Name }).ToListAsync(ct);
            Add("Dönem Referansları", ["Dönem"], periods.Select(x => new object?[] { x.Name }));
            var criteria = (await kpis.GetAllAsync(ct)).Where(x => x.IsActive);
            Add("KPI Referansları", ["KPI", "Ekipler"], criteria.Select(x => new object?[] { x.Title,
                x.IsCommon ? "Herkes" : string.Join("; ", x.Professions?.Count > 0 ? x.Professions.Select(p => p.Name) : new[] { x.ProfessionName ?? "" }) }));
        }
    }
}
