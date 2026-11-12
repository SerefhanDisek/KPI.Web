using KpiEvaluation.Application;
using KpiEvaluation.Domain;

namespace KpiEvaluation.Infrastructure;

internal static class ApprovedManagerResults
{
    public static IReadOnlyList<CalibrationRowDto> Create(IReadOnlyList<EvaluationSummaryDto> evaluations) =>
        evaluations.Where(x => x.Status == EvaluationStatus.Approved).GroupBy(x => new { x.EmployeeId, x.EvaluationPeriodId })
            .Select(group =>
            {
                var row = group.OrderBy(x => x.Id).First();
                var managers = row.Managers ?? [];
                var sameTeam = group.Select(x => x.ProfessionId).Distinct().Count() == 1;
                var valid = sameTeam && managers.Count > 0 && managers.All(x => x.WeightPercent > 0)
                    && managers.Select(x => x.ManagerUserId).Distinct().Count() == managers.Count
                    && managers.Sum(x => x.WeightPercent) == 100;
                var matches = managers.Select(manager => new { Manager = manager,
                    Evaluations = group.Where(x => x.EvaluatorUserId == manager.ManagerUserId).ToArray() }).ToArray();
                var complete = valid && matches.All(x => x.Evaluations.Length == 1);
                var status = !sameTeam ? "Aynı dönemin değerlendirmeleri farklı ekiplere ait; birleşik sonuç hesaplanamadı"
                    : !valid ? "Ekip amirleri ve toplam %100 ağırlıkları Yetkilendirme ekranında tanımlanmalı"
                    : matches.Any(x => x.Evaluations.Length > 1) ? "Aynı amire ait birden fazla onaylı değerlendirme var"
                    : complete ? "Tamamlandı"
                    : $"Onaylı değerlendirmesi beklenen amirler: {string.Join(", ", matches.Where(x => x.Evaluations.Length == 0).Select(x => x.Manager.ManagerName))}";
                return new CalibrationRowDto(group.Key.EmployeeId, row.EmployeeName, group.Key.EvaluationPeriodId,
                    row.PeriodName, row.ProfessionName, group.Max(x => x.ScorePercentage) - group.Min(x => x.ScorePercentage),
                    group.OrderBy(x => managers.SingleOrDefault(manager => manager.ManagerUserId == x.EvaluatorUserId)?.Position ?? int.MaxValue)
                        .ThenBy(x => x.EvaluatorName).ThenBy(x => x.Id)
                        .Select(x => new CalibrationScoreDto(x.Id, x.EvaluatorUserId, x.EvaluatorName ?? "Eski kayıt", x.ScorePercentage,
                            managers.SingleOrDefault(manager => manager.ManagerUserId == x.EvaluatorUserId)?.Position,
                            managers.SingleOrDefault(manager => manager.ManagerUserId == x.EvaluatorUserId)?.WeightPercent)).ToList(),
                    complete ? Math.Round(matches.Sum(x => x.Evaluations[0].ScorePercentage * x.Manager.WeightPercent / 100), 2) : null, status);
            })
            .OrderByDescending(x => x.CombinedPercentage).ThenBy(x => x.EmployeeName).ThenBy(x => x.PeriodId).ToList();
}
