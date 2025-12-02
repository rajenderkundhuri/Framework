using System.Reflection;
using Framework.Application.Events;
using Framework.Domain.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.Infrastructure.Events;

/// <summary>
/// Extension methods for configuring event services
/// </summary>
public static class EventExtensions
{
    /// <summary>
    /// Adds event dispatching services
    /// </summary>
    public static IServiceCollection AddEventDispatcher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EventSettings>(configuration.GetSection(EventSettings.SectionName));
        services.AddScoped<IEventDispatcher, InMemoryEventDispatcher>();
        return services;
    }

    /// <summary>
    /// Adds event dispatching with default settings
    /// </summary>
    public static IServiceCollection AddEventDispatcher(this IServiceCollection services)
    {
        services.Configure<EventSettings>(_ => { });
        services.AddScoped<IEventDispatcher, InMemoryEventDispatcher>();
        return services;
    }

    /// <summary>
    /// Registers all event handlers from an assembly
    /// </summary>
    public static IServiceCollection AddEventHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>)));

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>));

            foreach (var @interface in interfaces)
            {
                services.AddScoped(@interface, handlerType);
            }
        }

        return services;
    }

    /// <summary>
    /// Registers a specific event handler
    /// </summary>
    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IDomainEvent
        where THandler : class, IEventHandler<TEvent>
    {
        services.AddScoped<IEventHandler<TEvent>, THandler>();
        return services;
    }
}
