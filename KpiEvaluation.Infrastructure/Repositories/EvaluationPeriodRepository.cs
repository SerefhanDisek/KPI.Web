using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using KpiEvaluation.Application.Repositories;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class EvaluationPeriodRepository(KpiDbContext db) : IEvaluationPeriodRepository
{
    public async Task<IReadOnlyList<PeriodDto>> GetAllAsync(bool activeOnly=false,CancellationToken ct=default)=>await db.EvaluationPeriods.AsNoTracking().Where(x=>!activeOnly||x.IsActive).OrderByDescending(x=>x.StartDate).Select(x=>new PeriodDto(x.Id,x.Name,x.StartDate,x.EndDate,x.IsActive)).ToListAsync(ct);
    public async Task<PeriodDto> SaveAsync(int? id,string name,DateOnly start,DateOnly end,bool isActive,CancellationToken ct=default)
    {
        name=name.Trim();if(string.IsNullOrWhiteSpace(name))throw new ValidationException("Dönem adı boş olamaz.");if(end<start)throw new ValidationException("Bitiş tarihi başlangıç tarihinden küçük olamaz.");
        EvaluationPeriod e;if(id is null){e=new EvaluationPeriod{Name=name,StartDate=start,EndDate=end,IsActive=isActive};db.Add(e);}else{e=await db.EvaluationPeriods.FindAsync([id.Value],ct)??throw new ValidationException("Dönem bulunamadı.");e.Name=name;e.StartDate=start;e.EndDate=end;e.IsActive=isActive;}
        await db.SaveChangesAsync(ct);return new(e.Id,e.Name,e.StartDate,e.EndDate,e.IsActive);
    }
}
