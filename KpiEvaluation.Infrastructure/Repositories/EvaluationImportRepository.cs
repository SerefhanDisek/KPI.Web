using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using KpiEvaluation.Application.Repositories;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class EvaluationImportRepository(KpiDbContext db, IEvaluationRepository evaluationService) : IEvaluationImportRepository
{
    public async Task<ImportResultDto> ImportAsync(IReadOnlyList<PortableEvaluationDto> evaluations, CancellationToken ct = default)
    {
        var imported = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var source in evaluations)
        {
            try
            {
                var employee = await db.Employees.AsNoTracking()
                    .Where(x => x.IsActive && x.FullName == source.EmployeeName && x.Profession.Name == source.ProfessionName)
                    .Select(x => new { x.Id, x.ProfessionId }).SingleOrDefaultAsync(ct)
                    ?? throw new ValidationException($"Çalışan/meslek eşleşmedi: {source.EmployeeName} - {source.ProfessionName}");
                var periodId = await db.EvaluationPeriods.AsNoTracking()
                    .Where(x => x.IsActive && x.Name == source.PeriodName).Select(x => (int?)x.Id).SingleOrDefaultAsync(ct)
                    ?? throw new ValidationException($"Dönem bulunamadı: {source.PeriodName}");

                if (await db.EmployeeEvaluations.AnyAsync(x => x.EmployeeId == employee.Id && x.EvaluationPeriodId == periodId, ct))
                {
                    skipped++;
                    continue;
                }

                var criteria = await db.KpiCriteria.AsNoTracking()
                    .Where(x => x.IsActive && (x.IsCommon || x.ProfessionId == employee.ProfessionId))
                    .Select(x => new { x.Id, x.Title, x.IsCommon }).ToListAsync(ct);
                var sourceItems = source.Items.ToDictionary(x => (x.KpiTitle.Trim(), x.IsCommon));
                if (criteria.Count != sourceItems.Count || criteria.Any(x => !sourceItems.ContainsKey((x.Title, x.IsCommon))))
                    throw new ValidationException($"KPI listesi güncel kayıtlarla eşleşmedi: {source.EmployeeName} / {source.PeriodName}");

                var items = criteria.Select(x =>
                {
                    var item = sourceItems[(x.Title, x.IsCommon)];
                    return new EvaluationItemInput(x.Id,item.Score,item.Note,item.ActualValue);
                }).ToList();
                await evaluationService.SaveAsync(new EvaluationSaveInput(employee.Id,periodId,source.GeneralNote,items,source.EvaluationDate,source.Status,source.ManagerFeedback,source.EmployeeFeedback,source.DevelopmentAction,source.FollowUpDate),ct);
                imported++;
            }
            catch (Exception ex) when (ex is ValidationException or ArgumentException)
            {
                errors.Add($"{source.EmployeeName} / {source.PeriodName}: {ex.Message}");
            }
        }

        return new ImportResultDto(imported, skipped, errors);
    }
}
