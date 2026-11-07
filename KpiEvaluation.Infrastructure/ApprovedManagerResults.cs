using KpiEvaluation.Application;
using KpiEvaluation.Domain;

namespace KpiEvaluation.Infrastructure;

internal static class ApprovedManagerResults
{
    public static IReadOnlyList<CalibrationRowDto> Create(IReadOnlyList<EvaluationSummaryDto> evaluations) =>
        evaluations.Where(x => x.Status == EvaluationStatus.Approved).GroupBy(x => new { x.EmployeeId, x.EvaluationPeriodId })
            .Select(group =>
            {
                var first = group.Where(x => x.EvaluatorManagerLevel == 1).ToArray();
                var second = group.Where(x => x.EvaluatorManagerLevel == 2).ToArray();
                var complete = first.Length == 1 && second.Length == 1
                    && first[0].EvaluatorUserId is not null && second[0].EvaluatorUserId is not null
                    && first[0].EvaluatorUserId != second[0].EvaluatorUserId;
                var status = complete ? "Tamamlandı" : first.Length > 1 || second.Length > 1
                    ? "Aynı amir sırasında birden fazla onaylı değerlendirme var"
                    : first.Length == 0 && second.Length == 0 ? "Amir sırası belirlenmiş onaylı değerlendirmeler bekleniyor"
                    : first.Length == 0 ? "1. amirin onaylı değerlendirmesi bekleniyor" : "2. amirin onaylı değerlendirmesi bekleniyor";
                var row = group.OrderBy(x => x.Id).First();
                return new CalibrationRowDto(group.Key.EmployeeId, row.EmployeeName, group.Key.EvaluationPeriodId,
                    row.PeriodName, row.ProfessionName, group.Max(x => x.ScorePercentage) - group.Min(x => x.ScorePercentage),
                    group.OrderBy(x => x.EvaluatorManagerLevel ?? 3).ThenBy(x => x.EvaluatorName).ThenBy(x => x.Id)
                        .Select(x => new CalibrationScoreDto(x.Id, x.EvaluatorUserId, x.EvaluatorName ?? "Eski kayıt", x.ScorePercentage, x.EvaluatorManagerLevel)).ToList(),
                    complete ? Math.Round(first[0].ScorePercentage * 0.70m + second[0].ScorePercentage * 0.30m, 2) : null, status);
            })
            .OrderByDescending(x => x.CombinedPercentage).ThenBy(x => x.EmployeeName).ThenBy(x => x.PeriodId).ToList();
}
