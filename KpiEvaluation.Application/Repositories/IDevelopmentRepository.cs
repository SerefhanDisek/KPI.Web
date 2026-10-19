namespace KpiEvaluation.Application.Repositories;

public interface IDevelopmentRepository
{
    Task<CompetencyMatrixDto> GetMatrixAsync(CancellationToken cancellationToken);
    Task<CompetencyMatrixEntryDto> SaveMatrixAsync(CompetencyMatrixSaveInput input, CancellationToken cancellationToken);
    Task<DevelopmentBoardDto> GetBoardAsync(int? employeeId, bool activeOnly, int page, int pageSize, int userId, CancellationToken cancellationToken);
    Task<DevelopmentItemDto> SaveItemAsync(DevelopmentItemSaveInput input, int userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DevelopmentCheckInDto>> GetCheckInsAsync(long id, int userId, CancellationToken cancellationToken);
    Task AddCheckInAsync(long id, string note, decimal progress, int expectedVersion, int userId, CancellationToken cancellationToken);
}
