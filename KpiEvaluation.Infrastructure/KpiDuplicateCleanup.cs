using System.Text.Json;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;

namespace KpiEvaluation.Infrastructure;

public interface IKpiDuplicateCleanup
{
    Task<int> CleanupAsync(CancellationToken cancellationToken = default);
}

public sealed class KpiDuplicateCleanup(KpiDbContext db) : IKpiDuplicateCleanup
{
    public const string Version = "kpi-duplicate-cleanup-20261112-v1";

    public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        if (await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM __KpiSeedHistory WHERE ScriptName={Version}")
            .SingleAsync(cancellationToken) != 0) return 0;

        var criteria = await db.KpiCriteria.Where(x => x.IsActive).Include(x => x.Professions).ToListAsync(cancellationToken);
        var usage = await db.EmployeeEvaluationDetails.GroupBy(x => x.KpiCriterionId)
            .Select(x => new { Id = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);
        var count = 0;
        foreach (var group in criteria.GroupBy(DefinitionKey).Where(x => x.Count() > 1))
        {
            // Missing tags may be historical copies. Keep a record already containing all known tags;
            // conflicting goal/category values remain separate and no metadata is overwritten.
            var keeper = group.Where(candidate => group.All(other =>
                    (other.CompanyGoalId is null || other.CompanyGoalId == candidate.CompanyGoalId)
                    && (string.IsNullOrWhiteSpace(other.Category) || other.Category.Trim() == candidate.Category?.Trim())))
                .OrderByDescending(x => usage.GetValueOrDefault(x.Id)).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id).FirstOrDefault();
            if (keeper is null) continue;
            foreach (var duplicate in group.Where(x => x.Id != keeper.Id))
            {
                duplicate.IsActive = false;
                duplicate.UpdatedAt = DateTime.UtcNow;
                count++;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO __KpiSeedHistory(ScriptName) VALUES({Version})", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return count;
    }

    private static string DefinitionKey(KpiCriterion item) => JsonSerializer.Serialize(new
    {
        Title = KpiQuestionText.NormalizeIdentity(item.Title), item.IsCommon,
        Teams = item.Professions.Select(x => x.ProfessionId).Concat(item.ProfessionId is { } id ? [id] : []).Distinct().Order().ToArray(),
        Description = item.Description?.Trim() ?? "",
        item.MeasurementType, Unit = item.Unit?.Trim() ?? "", item.TargetValue, item.TargetDirection, item.WeightPercent,
        DataSource = item.DataSource?.Trim() ?? "", item.ReviewFrequency, item.ValidFrom, item.ValidTo,
        item.IsSpecific, item.IsMeasurable, item.IsAchievable, item.IsRelevant, item.IsTimeBound
    });
}
