using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authentication.Commands.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;
