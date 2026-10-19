using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using KpiEvaluation.Application.Repositories;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class CompanyGoalRepository(KpiDbContext db) : ICompanyGoalRepository
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
