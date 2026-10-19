using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using KpiEvaluation.Application.Repositories;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class PageDefinitionRepository(KpiDbContext db) : IPageDefinitionRepository
{
    public async Task<IReadOnlyList<PageDefinitionDto>> GetAccessibleAsync(int userId, CancellationToken ct = default)
    {
        var roleId = await db.AppUsers.AsNoTracking().Where(x => x.Id == userId && x.IsActive).Select(x => (int?)x.RoleId).SingleOrDefaultAsync(ct)
            ?? throw new AuthorizationException();
        var permissions = db.RolePermissions.AsNoTracking().Where(x => x.RoleId == roleId && x.IsAllowed).Select(x => x.Permission);
        return await db.PageDefinitions.AsNoTracking().Where(x => x.IsActive && permissions.Contains(x.RequiredPermission)).OrderBy(x => x.Title)
            .Select(x => new PageDefinitionDto(x.Id, x.Title, x.Slug, x.Description, x.LayoutJson, x.RequiredPermission, x.IsActive)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PageDefinitionDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.PageDefinitions.AsNoTracking().OrderBy(x => x.Title)
            .Select(x => new PageDefinitionDto(x.Id, x.Title, x.Slug, x.Description, x.LayoutJson, x.RequiredPermission, x.IsActive)).ToListAsync(ct);

    public async Task<PageDefinitionDto> SaveAsync(int? id, string title, string slug, string? description, string layoutJson,
        AppPermission requiredPermission, bool isActive, CancellationToken ct = default)
    {
        title = title.Trim(); slug = slug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(title) || !Regex.IsMatch(slug, "^[a-z0-9-]+$")) throw new ValidationException("Başlık zorunludur; adres yalnızca küçük harf, rakam ve tire içerebilir.");
        try { using var _ = JsonDocument.Parse(layoutJson); } catch (JsonException) { throw new ValidationException("Sayfa bileşenleri geçerli değil."); }
        if (await db.PageDefinitions.AnyAsync(x => x.Slug == slug && x.Id != id, ct)) throw new ValidationException("Bu sayfa adresi zaten kullanılıyor.");
        PageDefinition page;
        if (id is null) { page = new PageDefinition { Title = title, Slug = slug, LayoutJson = layoutJson }; db.PageDefinitions.Add(page); }
        else page = await db.PageDefinitions.FindAsync([id.Value], ct) ?? throw new ValidationException("Sayfa bulunamadı.");
        page.Title = title; page.Slug = slug; page.Description = description?.Trim(); page.LayoutJson = layoutJson;
        page.RequiredPermission = requiredPermission; page.IsActive = isActive; page.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new(page.Id, page.Title, page.Slug, page.Description, page.LayoutJson, page.RequiredPermission, page.IsActive);
    }
}
