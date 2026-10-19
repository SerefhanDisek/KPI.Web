using KpiEvaluation.Domain;

namespace KpiEvaluation.Application.Repositories;

public interface IAuthenticationRepository
{
    Task<AuthenticatedUserDto?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}
