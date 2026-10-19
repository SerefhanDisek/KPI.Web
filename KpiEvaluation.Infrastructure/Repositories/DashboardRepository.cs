using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using KpiEvaluation.Application.Repositories;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class DashboardRepository(KpiDbContext db) : IDashboardRepository
{
    public async Task<DashboardDto> GetAsync(int? periodId,CancellationToken ct=default,int? requesterUserId=null,int? evaluatorUserId=null)
    {
        var employeeQuery = db.Employees.AsNoTracking().Where(x => x.IsActive);
        if (requesterUserId is not null)
        {
            var user = await db.AppUsers.AsNoTracking().Where(x => x.Id == requesterUserId && x.IsActive)
                .Select(x => new { RoleCode=x.Role.Code, x.EmployeeId }).SingleOrDefaultAsync(ct) ?? throw new AuthorizationException();
            employeeQuery = user.RoleCode switch
            {
                RoleCodes.Administrator or RoleCodes.HumanResources => employeeQuery,
                RoleCodes.FirstManager => employeeQuery.Where(x => x.FirstManagerUserId == requesterUserId),
                RoleCodes.SecondManager => employeeQuery.Where(x => x.SecondManagerUserId == requesterUserId),
                RoleCodes.Employee => employeeQuery.Where(x => x.Id == user.EmployeeId),
                _ => employeeQuery.Where(_ => false)
            };
        }
        var total = await employeeQuery.CountAsync(ct);
        var employeeIds = employeeQuery.Select(x => x.Id);
        var query = db.EmployeeEvaluations.AsNoTracking()
            .Where(x => employeeIds.Contains(x.EmployeeId) && x.Status == EvaluationStatus.Approved
                && (periodId == null || x.EvaluationPeriodId == periodId)
                && (evaluatorUserId == null || x.EvaluatorUserId == evaluatorUserId));
        var rows = await query.OrderByDescending(x => x.ScorePercentage)
            .Select(x => new EvaluationSummaryDto(x.Id, x.EmployeeNameSnapshot, x.ProfessionNameSnapshot,
                x.PeriodNameSnapshot,x.TotalScore,x.AverageScore,x.EvaluationDate,x.GeneralNote,x.WeightedScore,x.Status,x.ManagerFeedback,x.EmployeeFeedback,x.DevelopmentAction,x.FollowUpDate,
                x.MaximumScore,x.ScorePercentage,x.EvaluatorUserId,x.EvaluatorUser==null?null:x.EvaluatorUser.DisplayName,x.ApprovedAt,false,false,x.EmployeeId,x.ProfessionIdSnapshot,x.EvaluationPeriodId))
            .ToListAsync(ct);
        var evaluated = await query.Select(x => x.EmployeeId).Distinct().CountAsync(ct);
        var goalRows=await db.EmployeeEvaluationDetails.AsNoTracking()
            .Where(x=>x.KpiCriterion.CompanyGoalId!=null&&employeeIds.Contains(x.EmployeeEvaluation.EmployeeId)&&x.EmployeeEvaluation.Status==EvaluationStatus.Approved&&(periodId==null||x.EmployeeEvaluation.EvaluationPeriodId==periodId))
            .Select(x=>new{GoalName=x.KpiCriterion.CompanyGoal!.Name,x.Score,x.EmployeeEvaluationId}).ToListAsync(ct);
        var goalProgress=goalRows.GroupBy(x=>x.GoalName).Select(x=>new GoalProgressDto(x.Key,Math.Round(x.Average(y=>(decimal)y.Score),2),x.Select(y=>y.EmployeeEvaluationId).Distinct().Count())).OrderByDescending(x=>x.AverageScore).ToList();
        return new(total, evaluated, Math.Max(0, total - evaluated),
            rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.WeightedScore), 2), rows,goalProgress,
            rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.ScorePercentage), 2));
    }

    public async Task<IReadOnlyList<PortableEvaluationDto>> GetPortableEvaluationsAsync(int? periodId, CancellationToken ct = default) =>
        await db.EmployeeEvaluations.AsNoTracking()
            .Where(x => x.Employee.IsActive && (periodId == null || x.EvaluationPeriodId == periodId))
            .OrderBy(x => x.EmployeeNameSnapshot).ThenBy(x => x.PeriodNameSnapshot)
            .Select(x => new PortableEvaluationDto(
                x.EmployeeNameSnapshot, x.ProfessionNameSnapshot, x.PeriodNameSnapshot, x.EvaluationDate, x.GeneralNote,
                x.Details.OrderBy(d => d.IsCommonSnapshot).ThenBy(d => d.DisplayOrderSnapshot)
                    .Select(d => new PortableEvaluationItemDto(d.KpiTitleSnapshot,d.Score,d.Note,d.IsCommonSnapshot,d.ActualValue)).ToList(),
                x.Status,x.ManagerFeedback,x.EmployeeFeedback,x.DevelopmentAction,x.FollowUpDate))
            .ToListAsync(ct);
}
