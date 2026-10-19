using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authentication.Commands.Login;

public sealed record LoginCommand(string UserName, string Password) : IRequest<AuthenticatedUserDto?>;
