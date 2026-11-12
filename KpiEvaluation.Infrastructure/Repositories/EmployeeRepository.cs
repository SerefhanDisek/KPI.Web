using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using KpiEvaluation.Application.Repositories;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class EmployeeRepository(KpiDbContext db) : IEmployeeRepository
{
    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Employees.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ValidationException("Çalışan bulunamadı.");
        if (!entity.IsActive) return;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Employees.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ValidationException("Çalışan bulunamadı.");
        const string usedMessage = "Çalışan başka kayıtlarda kullanıldığı için silinemez. Pasife alabilirsiniz.";
        if (await db.EmployeeEvaluations.AnyAsync(x => x.EmployeeId == id, ct)
            || await db.AppUsers.AnyAsync(x => x.EmployeeId == id, ct)
            || await db.EmployeeLeaves.AnyAsync(x => x.EmployeeId == id, ct)
            || await db.EmployeeLeaveBalances.AnyAsync(x => x.EmployeeId == id, ct)
            || await db.DevelopmentItems.AnyAsync(x => x.EmployeeId == id, ct))
            throw new ValidationException(usedMessage);
        db.Employees.Remove(entity);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
        {
            throw new ValidationException(usedMessage);
        }
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(bool activeOnly=false, CancellationToken ct=default) => await db.Employees.AsNoTracking().Where(x=>!activeOnly||x.IsActive).OrderBy(x=>x.FullName).Select(x=>new EmployeeDto(x.Id,x.FullName,x.ProfessionId,x.Profession.Name,x.IsActive,x.FirstManagerUserId,x.FirstManagerUser==null?null:x.FirstManagerUser.DisplayName,x.SecondManagerUserId,x.SecondManagerUser==null?null:x.SecondManagerUser.DisplayName)).ToListAsync(ct);
    public async Task<IReadOnlyList<LookupDto>> GetProfessionsAsync(CancellationToken ct=default) => await db.Professions.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).Select(x=>new LookupDto(x.Id,x.Name)).ToListAsync(ct);
    public async Task<EmployeeDto> SaveAsync(int? id,string fullName,int professionId,bool isActive,CancellationToken ct=default)
    {
        fullName=fullName.Trim(); if(string.IsNullOrWhiteSpace(fullName)) throw new ValidationException("Çalışan adı boş olamaz.");
        await using var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct) : null;
        var team = await db.Professions.AsNoTracking().Where(x => x.Id == professionId && x.IsActive)
            .Select(x => new { x.Name, x.FirstManagerUserId, x.SecondManagerUserId }).SingleOrDefaultAsync(ct)
            ?? throw new ValidationException("Geçerli bir meslek seçmelisiniz.");
        Employee entity;
        var inheritManagers = id is null;
        if(id is null) { entity=new Employee{FullName=fullName,ProfessionId=professionId,IsActive=isActive}; db.Employees.Add(entity); }
        else { entity=await db.Employees.FindAsync([id.Value],ct)??throw new ValidationException("Çalışan bulunamadı."); inheritManagers=entity.ProfessionId!=professionId; entity.FullName=fullName; entity.ProfessionId=professionId; entity.IsActive=isActive; entity.UpdatedAt=DateTime.UtcNow; }
        if (inheritManagers)
        {
            entity.FirstManagerUserId = team.FirstManagerUserId;
            entity.SecondManagerUserId = team.SecondManagerUserId;
        }
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return new(entity.Id,entity.FullName,entity.ProfessionId,team.Name,entity.IsActive);
    }
}
