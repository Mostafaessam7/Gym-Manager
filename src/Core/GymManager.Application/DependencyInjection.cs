using System.Reflection;
using FluentValidation;
using GymManager.Application.Abstractions;
using GymManager.Application.Cqrs;
using GymManager.Application.Services;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace GymManager.Application;

/// <summary>Composition root for the application layer: the CQRS dispatcher, handlers and validators.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddScoped<IBranchAccessGuard, BranchAccessGuard>();

        services.Scan(scan => scan.FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
