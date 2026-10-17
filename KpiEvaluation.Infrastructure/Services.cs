using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace KpiEvaluation.Infrastructure;

public sealed class EmployeeService(KpiDbContext db) : IEmployeeService
{
    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(bool activeOnly=false, CancellationToken ct=default) => await db.Employees.AsNoTracking().Where(x=>!activeOnly||x.IsActive).OrderBy(x=>x.FullName).Select(x=>new EmployeeDto(x.Id,x.FullName,x.ProfessionId,x.Profession.Name,x.IsActive,x.FirstManagerUserId,x.FirstManagerUser==null?null:x.FirstManagerUser.DisplayName,x.SecondManagerUserId,x.SecondManagerUser==null?null:x.SecondManagerUser.DisplayName)).ToListAsync(ct);
    public async Task<IReadOnlyList<LookupDto>> GetProfessionsAsync(CancellationToken ct=default) => await db.Professions.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).Select(x=>new LookupDto(x.Id,x.Name)).ToListAsync(ct);
    public async Task<EmployeeDto> SaveAsync(int? id,string fullName,int professionId,bool isActive,CancellationToken ct=default)
    {
        fullName=fullName.Trim(); if(string.IsNullOrWhiteSpace(fullName)) throw new ValidationException("Çalışan adı boş olamaz.");
        if(!await db.Professions.AnyAsync(x=>x.Id==professionId&&x.IsActive,ct)) throw new ValidationException("Geçerli bir meslek seçmelisiniz.");
        Employee entity;
        if(id is null) { entity=new Employee{FullName=fullName,ProfessionId=professionId,IsActive=isActive}; db.Employees.Add(entity); }
        else { entity=await db.Employees.FindAsync([id.Value],ct)??throw new ValidationException("Çalışan bulunamadı."); entity.FullName=fullName; entity.ProfessionId=professionId; entity.IsActive=isActive; entity.UpdatedAt=DateTime.UtcNow; }
        await db.SaveChangesAsync(ct); var profession=await db.Professions.Where(x=>x.Id==professionId).Select(x=>x.Name).SingleAsync(ct); return new(entity.Id,entity.FullName,entity.ProfessionId,profession,entity.IsActive);
    }
}

public sealed class ProfessionService(KpiDbContext db) : IProfessionService
{
    public async Task<IReadOnlyList<ProfessionDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Professions.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new ProfessionDto(x.Id, x.Name, x.IsActive)).ToListAsync(ct);

    public async Task<ProfessionDto> SaveAsync(int? id, string name, bool isActive, CancellationToken ct = default)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Meslek adı boş olamaz.");
        if (await db.Professions.AnyAsync(x => x.Name == name && x.Id != id, ct))
            throw new ValidationException("Bu meslek adı zaten kullanılıyor.");

        Profession entity;
        if (id is null)
        {
            entity = new Profession { Name = name, IsActive = isActive };
            db.Professions.Add(entity);
        }
        else
        {
            entity = await db.Professions.FindAsync([id.Value], ct)
                     ?? throw new ValidationException("Meslek bulunamadı.");
            entity.Name = name;
            entity.IsActive = isActive;
        }

        await db.SaveChangesAsync(ct);
        return new ProfessionDto(entity.Id, entity.Name, entity.IsActive);
    }
}

public sealed class CompanyGoalService(KpiDbContext db) : ICompanyGoalService
{
    public async Task<IReadOnlyList<CompanyGoalDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.CompanyGoals.AsNoTracking().OrderBy(x => x.Priority).ThenBy(x => x.Name)
            .Select(x => new CompanyGoalDto(x.Id, x.Name, x.Description, x.EvaluationPeriodId,
                x.EvaluationPeriod == null ? null : x.EvaluationPeriod.Name, x.Priority, x.IsActive)).ToListAsync(ct);

    public async Task<CompanyGoalDto> SaveAsync(int? id, string name, string? description, int? evaluationPeriodId, int priority, bool isActive, CancellationToken ct = default)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Şirket hedefi adı boş olamaz.");
        if (priority < 1) throw new ValidationException("Öncelik 1 veya daha büyük olmalıdır.");
        if (evaluationPeriodId is not null && !await db.EvaluationPeriods.AnyAsync(x => x.Id == evaluationPeriodId, ct))
            throw new ValidationException("Geçerli bir dönem seçmelisiniz.");
        if (await db.CompanyGoals.AnyAsync(x => x.Name == name && x.EvaluationPeriodId == evaluationPeriodId && x.Id != id, ct))
            throw new ValidationException("Bu hedef seçilen dönem için zaten tanımlı.");
        CompanyGoal entity;
        if (id is null) { entity = new CompanyGoal { Name = name }; db.CompanyGoals.Add(entity); }
        else entity = await db.CompanyGoals.FindAsync([id.Value], ct) ?? throw new ValidationException("Şirket hedefi bulunamadı.");
        entity.Name = name; entity.Description = description?.Trim(); entity.EvaluationPeriodId = evaluationPeriodId;
        entity.Priority = priority; entity.IsActive = isActive; entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        var periodName = evaluationPeriodId is null ? null : await db.EvaluationPeriods.Where(x => x.Id == evaluationPeriodId).Select(x => x.Name).SingleAsync(ct);
        return new(entity.Id, entity.Name, entity.Description, entity.EvaluationPeriodId, periodName, entity.Priority, entity.IsActive);
    }
}

public sealed class KpiService(KpiDbContext db) : IKpiService
{
    public async Task<IReadOnlyList<KpiDto>> GetAllAsync(CancellationToken ct=default) => await db.KpiCriteria
        .AsNoTracking()
        .OrderBy(x=>x.IsCommon)
        .ThenBy(x=>x.Profession == null ? string.Empty : x.Profession.Name)
        .ThenBy(x=>x.DisplayOrder)
        .Select(x=>new KpiDto(x.Id,x.Title,x.Description,x.ProfessionId,x.Profession==null?null:x.Profession.Name,x.IsCommon,x.DisplayOrder,x.IsActive,
            x.Category,x.CompanyGoalId,x.CompanyGoal==null?null:x.CompanyGoal.Name,x.MeasurementType,x.Unit,x.TargetValue,x.TargetDirection,x.WeightPercent,x.DataSource,x.ReviewFrequency,x.ValidFrom,x.ValidTo,x.IsSpecific,x.IsMeasurable,x.IsAchievable,x.IsRelevant,x.IsTimeBound,
            x.Professions.OrderBy(p=>p.Profession.Name).Select(p=>new LookupDto(p.ProfessionId,p.Profession.Name)).ToList()))
        .ToListAsync(ct);

    public async Task<IReadOnlyList<KpiDto>> GetForProfessionAsync(int professionId,int? evaluationPeriodId=null,CancellationToken ct=default)
    {
        EvaluationPeriod? period=evaluationPeriodId is null?null:await db.EvaluationPeriods.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==evaluationPeriodId,ct);
        return await db.KpiCriteria.AsNoTracking()
        .Where(x=>x.IsActive&&(x.IsCommon||x.ProfessionId==professionId||x.Professions.Any(p=>p.ProfessionId==professionId))
            &&(period==null||((x.ValidFrom==null||x.ValidFrom<=period.EndDate)&&(x.ValidTo==null||x.ValidTo>=period.StartDate))))
        .OrderBy(x=>x.IsCommon)
        .ThenBy(x=>x.DisplayOrder)
        .Select(x=>new KpiDto(x.Id,x.Title,x.Description,x.ProfessionId,x.Profession==null?null:x.Profession.Name,x.IsCommon,x.DisplayOrder,x.IsActive,
            x.Category,x.CompanyGoalId,x.CompanyGoal==null?null:x.CompanyGoal.Name,x.MeasurementType,x.Unit,x.TargetValue,x.TargetDirection,x.WeightPercent,x.DataSource,x.ReviewFrequency,x.ValidFrom,x.ValidTo,x.IsSpecific,x.IsMeasurable,x.IsAchievable,x.IsRelevant,x.IsTimeBound,
            x.Professions.OrderBy(p=>p.Profession.Name).Select(p=>new LookupDto(p.ProfessionId,p.Profession.Name)).ToList()))
        .ToListAsync(ct);
    }
    public async Task<KpiDto> SaveAsync(int? id,string title,string? description,int? professionId,bool isCommon,int displayOrder,bool isActive,CancellationToken ct=default,
        string? category=null,int? companyGoalId=null,KpiMeasurementType measurementType=KpiMeasurementType.ManualScore,string? unit=null,decimal? targetValue=null,
        KpiTargetDirection targetDirection=KpiTargetDirection.AtLeast,decimal weightPercent=0,string? dataSource=null,KpiReviewFrequency reviewFrequency=KpiReviewFrequency.SemiAnnual,
        DateOnly? validFrom=null,DateOnly? validTo=null,bool isSpecific=false,bool isMeasurable=false,bool isAchievable=false,bool isRelevant=false,bool isTimeBound=false,
        IReadOnlyList<int>? professionIds=null)
    {
        title=title.Trim(); if(string.IsNullOrWhiteSpace(title)) throw new ValidationException("KPI adı boş olamaz."); if(displayOrder<1) throw new ValidationException("Sıralama 1 veya daha büyük olmalıdır.");
        if(weightPercent is < 0 or > 100) throw new ValidationException("KPI ağırlığı 0 ile 100 arasında olmalıdır.");
        if(measurementType != KpiMeasurementType.ManualScore && targetValue is null) throw new ValidationException("Sayısal KPI için hedef değer girilmelidir.");
        if(validFrom is not null && validTo is not null && validTo < validFrom) throw new ValidationException("Geçerlilik bitişi başlangıçtan önce olamaz.");
        if(companyGoalId is not null && !await db.CompanyGoals.AnyAsync(x=>x.Id==companyGoalId&&x.IsActive,ct)) throw new ValidationException("Geçerli bir şirket hedefi seçmelisiniz.");
        var selectedIds = isCommon ? [] : (professionIds ?? (professionId is null ? [] : new[] { professionId.Value })).Distinct().Order().ToArray();
        if (!isCommon && selectedIds.Length == 0) throw new ValidationException("En az bir ekip (meslek grubu) seçmelisiniz.");
        if (await db.Professions.CountAsync(x => selectedIds.Contains(x.Id) && x.IsActive, ct) != selectedIds.Length)
            throw new ValidationException("Seçilen ekiplerden biri bulunamadı veya pasif durumda.");
        professionId = isCommon ? null : selectedIds[0];
        if (isActive)
        {
            var commonWeight=await db.KpiCriteria.Where(x=>x.IsActive&&x.IsCommon&&x.Id!=id).SumAsync(x=>(decimal?)x.WeightPercent,ct)??0;
            var specificTotals = await db.Professions.Where(p => isCommon || selectedIds.Contains(p.Id))
                .Select(p => db.KpiCriteria.Where(x => x.IsActive && !x.IsCommon && x.Id != id
                    && (x.ProfessionId == p.Id || x.Professions.Any(kp => kp.ProfessionId == p.Id)))
                    .Sum(x => (decimal?)x.WeightPercent) ?? 0).ToListAsync(ct);
            var scopeWeight = commonWeight + weightPercent + specificTotals.DefaultIfEmpty(0).Max();
            if(scopeWeight>100) throw new ValidationException($"Seçilen ekiplerden birinde aktif KPI ağırlıkları toplamı %{scopeWeight:0.##}. Toplam %100'ü aşamaz.");
        }
        KpiCriterion entity;
        if(id is null){entity=new KpiCriterion{Title=title,Description=description?.Trim(),ProfessionId=professionId,IsCommon=isCommon,DisplayOrder=displayOrder,IsActive=isActive};db.KpiCriteria.Add(entity);}
        else{entity=await db.KpiCriteria.Include(x=>x.Professions).SingleOrDefaultAsync(x=>x.Id==id.Value,ct)??throw new ValidationException("KPI bulunamadı.");entity.Title=title;entity.Description=description?.Trim();entity.ProfessionId=professionId;entity.IsCommon=isCommon;entity.DisplayOrder=displayOrder;entity.IsActive=isActive;entity.UpdatedAt=DateTime.UtcNow;}
        var currentIds = entity.Professions.Select(x => x.ProfessionId).ToHashSet();
        db.RemoveRange(entity.Professions.Where(x => !selectedIds.Contains(x.ProfessionId)).ToList());
        foreach (var selectedId in selectedIds.Where(x => !currentIds.Contains(x)))
            entity.Professions.Add(new KpiCriterionProfession { ProfessionId = selectedId });
        entity.Category=category?.Trim();entity.CompanyGoalId=companyGoalId;entity.MeasurementType=measurementType;entity.Unit=unit?.Trim();entity.TargetValue=targetValue;entity.TargetDirection=targetDirection;entity.WeightPercent=weightPercent;entity.DataSource=dataSource?.Trim();entity.ReviewFrequency=reviewFrequency;entity.ValidFrom=validFrom;entity.ValidTo=validTo;entity.IsSpecific=isSpecific;entity.IsMeasurable=isMeasurable;entity.IsAchievable=isAchievable;entity.IsRelevant=isRelevant;entity.IsTimeBound=isTimeBound;
        await db.SaveChangesAsync(ct); string? professionName=professionId is null?null:await db.Professions.Where(x=>x.Id==professionId).Select(x=>x.Name).SingleAsync(ct); string? goalName=companyGoalId is null?null:await db.CompanyGoals.Where(x=>x.Id==companyGoalId).Select(x=>x.Name).SingleAsync(ct);
        var professions = await db.Professions.AsNoTracking().Where(x => selectedIds.Contains(x.Id)).OrderBy(x => x.Name).Select(x => new LookupDto(x.Id, x.Name)).ToListAsync(ct);
        return new(entity.Id,entity.Title,entity.Description,entity.ProfessionId,professionName,entity.IsCommon,entity.DisplayOrder,entity.IsActive,entity.Category,entity.CompanyGoalId,goalName,entity.MeasurementType,entity.Unit,entity.TargetValue,entity.TargetDirection,entity.WeightPercent,entity.DataSource,entity.ReviewFrequency,entity.ValidFrom,entity.ValidTo,entity.IsSpecific,entity.IsMeasurable,entity.IsAchievable,entity.IsRelevant,entity.IsTimeBound,professions);
    }
}

public sealed class EvaluationPeriodService(KpiDbContext db) : IEvaluationPeriodService
{
    public async Task<IReadOnlyList<PeriodDto>> GetAllAsync(bool activeOnly=false,CancellationToken ct=default)=>await db.EvaluationPeriods.AsNoTracking().Where(x=>!activeOnly||x.IsActive).OrderByDescending(x=>x.StartDate).Select(x=>new PeriodDto(x.Id,x.Name,x.StartDate,x.EndDate,x.IsActive)).ToListAsync(ct);
    public async Task<PeriodDto> SaveAsync(int? id,string name,DateOnly start,DateOnly end,bool isActive,CancellationToken ct=default)
    {
        name=name.Trim();if(string.IsNullOrWhiteSpace(name))throw new ValidationException("Dönem adı boş olamaz.");if(end<start)throw new ValidationException("Bitiş tarihi başlangıç tarihinden küçük olamaz.");
        EvaluationPeriod e;if(id is null){e=new EvaluationPeriod{Name=name,StartDate=start,EndDate=end,IsActive=isActive};db.Add(e);}else{e=await db.EvaluationPeriods.FindAsync([id.Value],ct)??throw new ValidationException("Dönem bulunamadı.");e.Name=name;e.StartDate=start;e.EndDate=end;e.IsActive=isActive;}
        await db.SaveChangesAsync(ct);return new(e.Id,e.Name,e.StartDate,e.EndDate,e.IsActive);
    }
}

public sealed class EvaluationService(KpiDbContext db) : IEvaluationService
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

public sealed class DashboardService(KpiDbContext db) : IDashboardService
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
                x.MaximumScore,x.ScorePercentage,x.EvaluatorUserId,x.EvaluatorUser==null?null:x.EvaluatorUser.DisplayName,x.ApprovedAt))
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

public sealed class EvaluationImportService(KpiDbContext db, IEvaluationService evaluationService) : IEvaluationImportService
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
