using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using KpiEvaluation.Application.Repositories;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class EmployeeRepository(KpiDbContext db) : IEmployeeRepository
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
