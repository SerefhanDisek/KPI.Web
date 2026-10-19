using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using KpiEvaluation.Application.Repositories;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class EvaluationRepository(KpiDbContext db) : IEvaluationRepository
{
    public async Task SaveAsync(EvaluationSaveInput input, CancellationToken ct = default, int? evaluatorUserId = null)
    {
        if (evaluatorUserId is not null && input.Status is not (EvaluationStatus.Draft or EvaluationStatus.Completed or EvaluationStatus.Approved))
            throw new ValidationException("Geçici veya kalıcı kayıt seçmelisiniz.");
        var isDraft = evaluatorUserId is not null && input.Status == EvaluationStatus.Draft;
        ValidateScores(input.Items, isDraft);
        if (input.EmployeeId <= 0) throw new ValidationException("Çalışan seçmelisiniz.");
        if (input.EvaluationPeriodId <= 0) throw new ValidationException("Değerlendirme dönemi seçmelisiniz.");
        var employee = await db.Employees.AsNoTracking()
            .Where(x => x.Id == input.EmployeeId && x.IsActive)
            .Select(x => new { x.Id, x.ProfessionId, x.FullName, ProfessionName = x.Profession.Name })
            .SingleOrDefaultAsync(ct) ?? throw new ValidationException("Aktif çalışan bulunamadı.");
        await EnsureEmployeeAccessAsync(employee.Id, evaluatorUserId, ct);
        var period = await db.EvaluationPeriods.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == input.EvaluationPeriodId && x.IsActive, ct)
            ?? throw new ValidationException("Aktif değerlendirme dönemi bulunamadı.");
        var criteria = await db.KpiCriteria.AsNoTracking()
            .Where(x => x.IsActive && (x.IsCommon || x.ProfessionId == employee.ProfessionId || x.Professions.Any(p => p.ProfessionId == employee.ProfessionId))
                && (x.ValidFrom == null || x.ValidFrom <= period.EndDate) && (x.ValidTo == null || x.ValidTo >= period.StartDate))
            .Select(x => new { x.Id, x.Title, x.IsCommon, x.DisplayOrder, x.MeasurementType, x.Unit, x.TargetValue, x.TargetDirection, x.WeightPercent }).ToListAsync(ct);
        var criteriaById = criteria.ToDictionary(x => x.Id);
        if (input.Items.Count != criteria.Count || input.Items.Select(x => x.KpiCriterionId).Distinct().Count() != criteria.Count
            || input.Items.Any(x => !criteriaById.ContainsKey(x.KpiCriterionId)))
            throw new ValidationException("Mesleğe ait tüm güncel KPI maddeleri puanlanmalıdır.");
        foreach(var item in input.Items)
        {
            var criterion=criteriaById[item.KpiCriterionId];
            if(criterion.MeasurementType==KpiMeasurementType.ManualScore)continue;
            if (isDraft && item.Score == 0 && item.ActualValue is null) continue;
            if(item.ActualValue is null||criterion.TargetValue is null)throw new ValidationException($"{criterion.Title} için gerçekleşen değer girilmelidir.");
            if(item.Score!=CalculateMetricScore(item.ActualValue.Value,criterion.TargetValue.Value,criterion.TargetDirection))throw new ValidationException($"{criterion.Title} için hesaplanan puan geçersiz.");
        }

        var weightTotal = criteria.Sum(x => x.WeightPercent);
        var weightedScore = weightTotal > 0
            ? Math.Round(input.Items.Sum(x => x.Score * criteriaById[x.KpiCriterionId].WeightPercent) / weightTotal, 2)
            : Math.Round((decimal)input.Items.Average(x => x.Score), 2);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var evaluation = new EmployeeEvaluation
            {
                EmployeeId = employee.Id,
                EvaluationPeriodId = period.Id,
                EmployeeNameSnapshot = employee.FullName,
                ProfessionIdSnapshot = employee.ProfessionId,
                ProfessionNameSnapshot = employee.ProfessionName,
                PeriodNameSnapshot = period.Name,
                EvaluationDate = input.EvaluationDate ?? DateTime.UtcNow,
                TotalScore = input.Items.Sum(x => x.Score),
                AverageScore = Math.Round((decimal)input.Items.Average(x => x.Score), 2),
                WeightedScore = weightedScore,
                MaximumScore = input.Items.Count * 5,
                ScorePercentage = Math.Round(weightedScore * 20, 2),
                EvaluatorUserId = evaluatorUserId,
                GeneralNote = input.GeneralNote?.Trim(),
                ManagerFeedback=input.ManagerFeedback?.Trim(),EmployeeFeedback=input.EmployeeFeedback?.Trim(),DevelopmentAction=input.DevelopmentAction?.Trim(),FollowUpDate=input.FollowUpDate,
                Status=isDraft?EvaluationStatus.Draft:EvaluationStatus.Approved,
                ApprovedByUserId=isDraft?null:evaluatorUserId,
                ApprovedAt=isDraft?null:DateTime.UtcNow,
                Details = input.Items.Select(x =>
                {
                    var criterion = criteriaById[x.KpiCriterionId];
                    return new EmployeeEvaluationDetail
                    {
                        KpiCriterionId = x.KpiCriterionId,
                        KpiTitleSnapshot = criterion.Title,
                        IsCommonSnapshot = criterion.IsCommon,
                        DisplayOrderSnapshot = criterion.DisplayOrder,
                        Score = x.Score,
                        ActualValue = x.ActualValue,
                        WeightPercentSnapshot = criterion.WeightPercent,
                        TargetValueSnapshot = criterion.TargetValue,
                        TargetDirectionSnapshot = criterion.TargetDirection,
                        MeasurementTypeSnapshot = criterion.MeasurementType,
                        UnitSnapshot = criterion.Unit,
                        WeightedScore = weightTotal > 0 ? Math.Round(x.Score * criterion.WeightPercent / weightTotal, 4) : Math.Round((decimal)x.Score / criteria.Count, 4),
                        Note = x.Note?.Trim()
                    };
                }).ToList()
            };
            db.Add(evaluation);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await transaction.RollbackAsync(ct);
            throw new DuplicateEvaluationException();
        }
    }

    public async Task<IReadOnlyList<EvaluationSummaryDto>> GetHistoryAsync(int? employeeId, int? professionId, int? periodId, CancellationToken ct = default,
        int? requesterUserId = null, int? evaluatorUserId = null)
    {
        var roleCode = requesterUserId is null ? null : await db.AppUsers.AsNoTracking().Where(x => x.Id == requesterUserId)
            .Select(x => x.Role.Code).SingleOrDefaultAsync(ct);
        var canEdit = requesterUserId is not null && await db.RolePermissions.AnyAsync(x => x.Role.Users.Any(u => u.Id == requesterUserId && u.IsActive && u.Role.IsActive)
            && x.Permission == AppPermission.EvaluationCreate && x.IsAllowed, ct);
        var canApprove = requesterUserId is not null && await db.RolePermissions.AnyAsync(x => x.Role.Users.Any(u => u.Id == requesterUserId && u.IsActive && u.Role.IsActive)
            && x.Permission == AppPermission.EvaluationApprove && x.IsAllowed, ct);
        return await ApplyAccessFilter(db.EmployeeEvaluations.AsNoTracking(), requesterUserId)
            .Where(x => (employeeId == null || x.EmployeeId == employeeId)
                        && (professionId == null || x.ProfessionIdSnapshot == professionId)
                        && (periodId == null || x.EvaluationPeriodId == periodId)
                        && (evaluatorUserId == null || x.EvaluatorUserId == evaluatorUserId))
            .OrderByDescending(x => x.EvaluationDate)
            .Select(x => new EvaluationSummaryDto(x.Id, x.EmployeeNameSnapshot, x.ProfessionNameSnapshot,
                x.PeriodNameSnapshot,x.TotalScore,x.AverageScore,x.EvaluationDate,x.GeneralNote,x.WeightedScore,x.Status,x.ManagerFeedback,x.EmployeeFeedback,x.DevelopmentAction,x.FollowUpDate,
                x.MaximumScore,x.ScorePercentage,x.EvaluatorUserId,x.EvaluatorUser==null?null:x.EvaluatorUser.DisplayName,x.ApprovedAt,
                canEdit && x.Status != EvaluationStatus.Approved && (x.EvaluatorUserId == requesterUserId || roleCode == RoleCodes.Administrator),
                canApprove && x.Status == EvaluationStatus.Completed && (roleCode == RoleCodes.Administrator
                    || (roleCode == RoleCodes.FirstManager && x.Employee.FirstManagerUserId == requesterUserId)
                    || (roleCode == RoleCodes.SecondManager && x.Employee.SecondManagerUserId == requesterUserId))))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EvaluationDetailDto>> GetDetailsAsync(long id, CancellationToken ct = default, int? requesterUserId = null)
    {
        if (!await ApplyAccessFilter(db.EmployeeEvaluations.AsNoTracking(), requesterUserId).AnyAsync(x => x.Id == id, ct)) throw new AuthorizationException();
        return await db.EmployeeEvaluationDetails.AsNoTracking()
            .Where(x => x.EmployeeEvaluationId == id)
            .OrderBy(x => x.IsCommonSnapshot).ThenBy(x => x.DisplayOrderSnapshot)
            .Select(x => new EvaluationDetailDto(x.KpiTitleSnapshot, x.Score, x.Note, x.IsCommonSnapshot, x.ActualValue,
                x.WeightPercentSnapshot, x.TargetValueSnapshot, x.MeasurementTypeSnapshot, x.UnitSnapshot, x.WeightedScore, x.TargetDirectionSnapshot))
            .ToListAsync(ct);
    }

    public async Task<EvaluationEditDto> GetForEditAsync(long evaluationId, CancellationToken ct = default, int? requesterUserId = null)
    {
        var evaluation = await ApplyAccessFilter(db.EmployeeEvaluations.AsNoTracking(), requesterUserId)
            .Where(x => x.Id == evaluationId)
            .Select(x => new { x.Id, x.GeneralNote, x.Status, x.ManagerFeedback, x.EmployeeFeedback, x.DevelopmentAction, x.FollowUpDate, x.EvaluatorUserId })
            .SingleOrDefaultAsync(ct) ?? throw new ValidationException("Değerlendirme bulunamadı.");
        await EnsureEvaluationEditAccessAsync(evaluation.EvaluatorUserId, requesterUserId, evaluation.Status, ct);
        var items = await db.EmployeeEvaluationDetails.AsNoTracking()
            .Where(x => x.EmployeeEvaluationId == evaluationId)
            .OrderBy(x => x.IsCommonSnapshot).ThenBy(x => x.DisplayOrderSnapshot)
            .Select(x => new EvaluationEditItemDto(x.Id, x.KpiTitleSnapshot, x.Score, x.Note, x.IsCommonSnapshot,
                x.ActualValue, x.MeasurementTypeSnapshot, x.UnitSnapshot, x.TargetValueSnapshot, x.TargetDirectionSnapshot))
            .ToListAsync(ct);
        return new EvaluationEditDto(evaluation.Id, evaluation.GeneralNote, items, evaluation.Status, evaluation.ManagerFeedback,
            evaluation.EmployeeFeedback, evaluation.DevelopmentAction, evaluation.FollowUpDate);
    }

    public async Task UpdateAsync(long evaluationId, string? generalNote, IReadOnlyList<EvaluationItemUpdateInput> items, CancellationToken ct = default,
        EvaluationStatus status=EvaluationStatus.Completed,string? managerFeedback=null,string? employeeFeedback=null,string? developmentAction=null,DateOnly? followUpDate=null,int? requesterUserId=null)
    {
        if (requesterUserId is not null && status is not (EvaluationStatus.Draft or EvaluationStatus.Completed or EvaluationStatus.Approved))
            throw new ValidationException("Geçici veya kalıcı kayıt seçmelisiniz.");
        var isDraft = requesterUserId is not null && status == EvaluationStatus.Draft;
        if (items.Count == 0 || items.Any(x => x.Score < (isDraft ? 0 : 1) || x.Score > 5))
            throw new ValidationException("Bütün KPI maddeleri 1 ile 5 arasında puanlanmalıdır.");
        var evaluation = await db.EmployeeEvaluations.Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.Id == evaluationId, ct)
            ?? throw new ValidationException("Değerlendirme bulunamadı.");
        if (!await ApplyAccessFilter(db.EmployeeEvaluations.AsNoTracking(), requesterUserId).AnyAsync(x => x.Id == evaluationId, ct)) throw new AuthorizationException();
        await EnsureEvaluationEditAccessAsync(evaluation.EvaluatorUserId, requesterUserId, evaluation.Status, ct);
        if (items.Select(x => x.DetailId).Distinct().Count() != items.Count)
            throw new ValidationException("Değerlendirme maddeleri yinelenemez.");
        var updates = items.ToDictionary(x => x.DetailId);
        if (updates.Count != evaluation.Details.Count || evaluation.Details.Any(x => !updates.ContainsKey(x.Id)))
            throw new ValidationException("Değerlendirme maddeleri güncel kayıtla eşleşmiyor.");
        foreach (var detail in evaluation.Details)
        {
            var update = updates[detail.Id];
            if(detail.MeasurementTypeSnapshot!=KpiMeasurementType.ManualScore)
            {
                if (!(isDraft && update.Score == 0 && update.ActualValue is null))
                {
                    if(update.ActualValue is null||detail.TargetValueSnapshot is null)throw new ValidationException($"{detail.KpiTitleSnapshot} için gerçekleşen değer girilmelidir.");
                    if(update.Score!=CalculateMetricScore(update.ActualValue.Value,detail.TargetValueSnapshot.Value,detail.TargetDirectionSnapshot))throw new ValidationException($"{detail.KpiTitleSnapshot} için hesaplanan puan geçersiz.");
                }
            }
            detail.Score = update.Score;
            detail.ActualValue = update.ActualValue;
            detail.Note = update.Note?.Trim();
        }
        evaluation.GeneralNote = generalNote?.Trim();
        evaluation.TotalScore = items.Sum(x => x.Score);
        evaluation.AverageScore = Math.Round((decimal)items.Average(x => x.Score), 2);
        var weightTotal = evaluation.Details.Sum(x => x.WeightPercentSnapshot);
        evaluation.WeightedScore = weightTotal > 0
            ? Math.Round(evaluation.Details.Sum(x => x.Score * x.WeightPercentSnapshot) / weightTotal, 2)
            : evaluation.AverageScore;
        evaluation.MaximumScore = items.Count * 5;
        evaluation.ScorePercentage = Math.Round(evaluation.WeightedScore * 20, 2);
        foreach (var detail in evaluation.Details)
            detail.WeightedScore = weightTotal > 0 ? Math.Round(detail.Score * detail.WeightPercentSnapshot / weightTotal, 4) : Math.Round((decimal)detail.Score / evaluation.Details.Count, 4);
        evaluation.Status=isDraft?EvaluationStatus.Draft:EvaluationStatus.Approved;
        evaluation.ApprovedByUserId=isDraft?null:requesterUserId;
        evaluation.ApprovedAt=isDraft?null:DateTime.UtcNow;
        evaluation.ManagerFeedback=managerFeedback?.Trim();evaluation.EmployeeFeedback=employeeFeedback?.Trim();evaluation.DevelopmentAction=developmentAction?.Trim();evaluation.FollowUpDate=followUpDate;
        evaluation.UpdatedAt = DateTime.UtcNow;
        await SaveEvaluationChangesAsync(ct);
    }

    public async Task ApproveAsync(long evaluationId, int approverUserId, CancellationToken ct = default)
    {
        var evaluation = await db.EmployeeEvaluations.Include(x => x.Details).SingleOrDefaultAsync(x => x.Id == evaluationId, ct)
            ?? throw new ValidationException("Değerlendirme bulunamadı.");
        await EnsureEmployeeAccessAsync(evaluation.EmployeeId, approverUserId, ct);
        if (evaluation.Status == EvaluationStatus.Approved) return;
        if (evaluation.Status != EvaluationStatus.Completed)
            throw new ValidationException("Bu değerlendirme onaylanabilir durumda değil.");
        ValidateScores(evaluation.Details.Select(x => new EvaluationItemInput(x.KpiCriterionId, x.Score, x.Note, x.ActualValue)).ToList());
        evaluation.Status = EvaluationStatus.Approved;
        evaluation.ApprovedByUserId = approverUserId;
        evaluation.ApprovedAt = DateTime.UtcNow;
        evaluation.UpdatedAt = DateTime.UtcNow;
        await SaveEvaluationChangesAsync(ct);
    }

    private async Task SaveEvaluationChangesAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ValidationException("Değerlendirme başka bir işlemle güncellendi. Sayfayı yenileyip tekrar deneyin.");
        }
    }

    private async Task EnsureEvaluationEditAccessAsync(int? evaluatorUserId, int? requesterUserId, EvaluationStatus status, CancellationToken ct)
    {
        if (requesterUserId is null) return;
        if (evaluatorUserId != requesterUserId && !await db.AppUsers.AsNoTracking()
            .AnyAsync(x => x.Id == requesterUserId && x.IsActive && x.Role.IsActive && x.Role.Code == RoleCodes.Administrator, ct))
            throw new AuthorizationException("Başka bir amirin değerlendirmesini yalnızca görüntüleyebilirsiniz.");
        if (status == EvaluationStatus.Approved) throw new ValidationException("Onaylanmış değerlendirme değiştirilemez.");
    }

    private async Task EnsureEmployeeAccessAsync(int employeeId, int? userId, CancellationToken ct)
    {
        if (userId is null) return;
        var user = await db.AppUsers.AsNoTracking().Where(x => x.Id == userId && x.IsActive)
            .Select(x => new { RoleCode=x.Role.Code, x.EmployeeId }).SingleOrDefaultAsync(ct) ?? throw new AuthorizationException();
        if (user.RoleCode == RoleCodes.Administrator) return;
        var allowed = user.RoleCode switch
        {
            RoleCodes.FirstManager => await db.Employees.AnyAsync(x => x.Id == employeeId && x.FirstManagerUserId == userId, ct),
            RoleCodes.SecondManager => await db.Employees.AnyAsync(x => x.Id == employeeId && x.SecondManagerUserId == userId, ct),
            RoleCodes.Employee => user.EmployeeId == employeeId,
            _ => false
        };
        if (!allowed) throw new AuthorizationException("Bu çalışan için işlem yetkiniz bulunmuyor.");
    }

    private IQueryable<EmployeeEvaluation> ApplyAccessFilter(IQueryable<EmployeeEvaluation> query, int? userId)
    {
        if (userId is null) return query;
        var user = db.AppUsers.AsNoTracking().Where(x => x.Id == userId && x.IsActive)
            .Select(x => new { RoleCode=x.Role.Code, x.EmployeeId }).SingleOrDefault();
        if (user is null) return query.Where(_ => false);
        return user.RoleCode switch
        {
            RoleCodes.Administrator or RoleCodes.HumanResources => query,
            RoleCodes.FirstManager => query.Where(x => x.Employee.FirstManagerUserId == userId || x.EvaluatorUserId == userId),
            RoleCodes.SecondManager => query.Where(x => x.Employee.SecondManagerUserId == userId || x.EvaluatorUserId == userId),
            RoleCodes.Employee => query.Where(x => x.EmployeeId == user.EmployeeId && x.Status == EvaluationStatus.Approved),
            _ => query.Where(_ => false)
        };
    }

    private static void ValidateScores(IReadOnlyList<EvaluationItemInput> items, bool isDraft = false)
    {
        if (items.Count == 0) throw new ValidationException("Değerlendirilecek KPI bulunamadı.");
        if (items.Any(x => x.Score < (isDraft ? 0 : 1) || x.Score > 5))
            throw new ValidationException("Bütün KPI maddeleri 1 ile 5 arasında puanlanmalıdır.");
    }

    private static int CalculateMetricScore(decimal actual,decimal target,KpiTargetDirection direction)
    {
        if(direction==KpiTargetDirection.AtMost){if(target==0)return actual<=0?5:1;var r=actual/target;return r<=1?5:r<=1.1m?4:r<=1.25m?3:r<=1.5m?2:1;}
        if(target==0)return actual>=0?5:1;var a=actual/target;return a>=1?5:a>=.9m?4:a>=.75m?3:a>=.6m?2:1;
    }
}
