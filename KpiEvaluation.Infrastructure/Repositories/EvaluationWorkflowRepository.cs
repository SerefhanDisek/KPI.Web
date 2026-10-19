using KpiEvaluation.Application;
using KpiEvaluation.Application.Repositories;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class EvaluationWorkflowRepository(KpiDbContext db, IEvaluationRepository evaluations) : IEvaluationWorkflowRepository
{
    public async Task<IReadOnlyList<EmployeeResponseDto>> GetResponsesAsync(long evaluationId, int userId, CancellationToken ct)
    {
        await evaluations.GetDetailsAsync(evaluationId, ct, userId);
        return await db.EvaluationEmployeeResponses.AsNoTracking().Where(x => x.EvaluationId == evaluationId)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Select(x => new EmployeeResponseDto(x.Id, x.Decision, x.Comment, x.User.DisplayName, x.CreatedAt)).ToListAsync(ct);
    }

    public async Task RespondAsync(long evaluationId, EmployeeResponseDecision decision, string comment, int userId, CancellationToken ct)
    {
        if (!Enum.IsDefined(decision)) throw new ValidationException("Geçerli bir yanıt seçmelisiniz.");
        comment = comment.Trim();
        if (comment.Length > 2000 || (decision != EmployeeResponseDecision.Accepted && comment.Length == 0))
            throw new ValidationException("Görüş için açıklama zorunludur; en fazla 2000 karakter olabilir.");
        var employeeId = await db.AppUsers.AsNoTracking().Where(x => x.Id == userId && x.IsActive && x.Role.IsActive)
            .Select(x => x.EmployeeId).SingleOrDefaultAsync(ct);
        if (employeeId is null || !await db.EmployeeEvaluations.AnyAsync(x => x.Id == evaluationId && x.EmployeeId == employeeId
                && x.Status == EvaluationStatus.Approved, ct)) throw new AuthorizationException("Yalnızca kendi onaylı değerlendirmenize yanıt verebilirsiniz.");
        db.EvaluationEmployeeResponses.Add(new EvaluationEmployeeResponse { EvaluationId = evaluationId, UserId = userId, Decision = decision, Comment = comment });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EvaluationTaskDto>> GetTasksAsync(int? periodId, int userId, CancellationToken ct)
    {
        var admin = await db.AppUsers.AsNoTracking().AnyAsync(x => x.Id == userId && x.IsActive && x.Role.IsActive && x.Role.Code == RoleCodes.Administrator, ct);
        var employees = await db.Employees.AsNoTracking().Where(x => x.IsActive && (admin || x.FirstManagerUserId == userId || x.SecondManagerUserId == userId))
            .Select(x => new { x.Id, x.FullName, x.FirstManagerUserId, FirstName = x.FirstManagerUser == null ? null : x.FirstManagerUser.DisplayName,
                x.SecondManagerUserId, SecondName = x.SecondManagerUser == null ? null : x.SecondManagerUser.DisplayName }).ToListAsync(ct);
        var periods = await db.EvaluationPeriods.AsNoTracking().Where(x => x.IsActive && (periodId == null || x.Id == periodId)).ToListAsync(ct);
        var ids = employees.Select(x => x.Id).ToArray();
        var saved = await db.EmployeeEvaluations.AsNoTracking().Where(x => ids.Contains(x.EmployeeId) && (periodId == null || x.EvaluationPeriodId == periodId)
            && (admin || x.EvaluatorUserId == userId)).Select(x => new { x.Id, x.EmployeeId, x.EvaluationPeriodId, x.EvaluatorUserId, x.Status }).ToListAsync(ct);
        var tasks = new List<EvaluationTaskDto>();
        foreach (var employee in employees)
        foreach (var manager in new[] { (employee.FirstManagerUserId, employee.FirstName), (employee.SecondManagerUserId, employee.SecondName) }.Distinct())
        {
            if (manager.Item1 is not { } managerId || (!admin && managerId != userId)) continue;
            foreach (var period in periods)
            {
                var evaluation = saved.SingleOrDefault(x => x.EmployeeId == employee.Id && x.EvaluationPeriodId == period.Id && x.EvaluatorUserId == managerId);
                tasks.Add(new(employee.Id, employee.FullName, period.Id, period.Name, period.EndDate, managerId, manager.Item2 ?? "—", evaluation?.Id, evaluation?.Status));
            }
        }
        return tasks.OrderBy(x => x.Status == EvaluationStatus.Approved).ThenBy(x => x.PeriodEndDate).ThenBy(x => x.EmployeeName).ThenBy(x => x.EvaluatorUserId).ToList();
    }

    public async Task<IReadOnlyList<CalibrationRowDto>> GetCalibrationAsync(int? periodId, int userId, CancellationToken ct)
    {
        var rows = await evaluations.GetHistoryAsync(null, null, periodId, ct, userId);
        return rows.Where(x => x.Status == EvaluationStatus.Approved).GroupBy(x => new { x.EmployeeId, x.EvaluationPeriodId })
            .Select(group => new CalibrationRowDto(group.Key.EmployeeId, group.First().EmployeeName, group.Key.EvaluationPeriodId,
                group.First().PeriodName, group.First().ProfessionName, group.Max(x => x.ScorePercentage) - group.Min(x => x.ScorePercentage),
                group.OrderBy(x => x.EvaluatorName).ThenBy(x => x.Id).Select(x => new CalibrationScoreDto(x.Id, x.EvaluatorUserId, x.EvaluatorName ?? "Eski kayıt", x.ScorePercentage)).ToList()))
            .OrderByDescending(x => x.Difference).ThenBy(x => x.EmployeeName).ThenBy(x => x.PeriodId).ToList();
    }

    public async Task<AuditPageDto> GetAuditAsync(int page, int pageSize, string? entityType, CancellationToken ct)
    {
        if (page is < 1 or > 1_000_000 || pageSize is < 1 or > 100) throw new ValidationException("Geçerli sayfa ve 1–100 arasında sayfa boyutu seçmelisiniz.");
        var query = db.AuditEntries.AsNoTracking().Where(x => entityType == null || x.EntityType == entityType);
        var count = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AuditEntryDto(x.Id, x.UserId, db.AppUsers.Where(u => u.Id == x.UserId).Select(u => u.DisplayName).FirstOrDefault(),
                x.EntityType, x.EntityId, x.Action, x.ChangedFieldsJson, x.CreatedAt)).ToListAsync(ct);
        return new(rows, count, page, pageSize);
    }
}
