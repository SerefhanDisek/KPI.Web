using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using KpiEvaluation.Application.Repositories;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class ProfessionRepository(KpiDbContext db) : IProfessionRepository
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
