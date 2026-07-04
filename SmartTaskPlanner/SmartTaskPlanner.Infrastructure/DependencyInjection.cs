using Microsoft.Extensions.DependencyInjection;
using SmartTaskPlanner.Application.Interfaces;
using SmartTaskPlanner.Infrastructure.Repositories;

namespace SmartTaskPlanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();

        return services;
    }
}
