using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartTaskPlanner.Application.Interfaces;
using SmartTaskPlanner.Application.Services;
using SmartTaskPlanner.Application.Validators;

namespace SmartTaskPlanner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IGraphService, GraphService>();
        services.AddScoped<SmartTaskPlanner.Domain.Factories.ITaskFactory, SmartTaskPlanner.Domain.Factories.TaskFactory>();
        
        // Add FluentValidation
        services.AddValidatorsFromAssemblyContaining<CreateTaskDtoValidator>();

        return services;
    }
}
