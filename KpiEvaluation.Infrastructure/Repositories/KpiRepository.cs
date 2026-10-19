using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using KpiEvaluation.Application.Repositories;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class KpiRepository(KpiDbContext db) : IKpiRepository
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
