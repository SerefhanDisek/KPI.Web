using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authentication.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<AuthenticatedUserDto>;
