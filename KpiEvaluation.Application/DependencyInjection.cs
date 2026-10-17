using KpiEvaluation.Application.Features;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace KpiEvaluation.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<GetDashboardQuery>());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));

        return services;
    }
}
