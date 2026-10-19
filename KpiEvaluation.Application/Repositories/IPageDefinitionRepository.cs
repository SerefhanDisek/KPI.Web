using KpiEvaluation.Domain;

namespace KpiEvaluation.Application.Repositories;

public interface IPageDefinitionRepository
{
    Task<IReadOnlyList<PageDefinitionDto>> GetAccessibleAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PageDefinitionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PageDefinitionDto> SaveAsync(int? id, string title, string slug, string? description, string layoutJson,
        AppPermission requiredPermission, bool isActive, CancellationToken cancellationToken = default);
}
