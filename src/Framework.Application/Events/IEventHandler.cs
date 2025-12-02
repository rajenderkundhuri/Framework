using Framework.Domain.Events;

namespace Framework.Application.Events;

/// <summary>
/// Handler for domain events
/// </summary>
/// <typeparam name="TEvent">Event type</typeparam>
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the event
    /// </summary>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handler that can handle multiple event types
/// </summary>
public interface IEventHandler
{
    /// <summary>
    /// Checks if this handler can handle the event type
    /// </summary>
    bool CanHandle(Type eventType);

    /// <summary>
    /// Handles the event
    /// </summary>
    Task HandleAsync(IDomainEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dispatcher for domain events
/// </summary>
public interface IEventDispatcher
{
    /// <summary>
    /// Dispatches an event to all registered handlers
    /// </summary>
    Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    /// <summary>
    /// Dispatches multiple events
    /// </summary>
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}

/// <summary>
/// Event publisher for external/integration events
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event to external subscribers
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}

/// <summary>
/// Event subscriber for receiving external events
/// </summary>
public interface IEventSubscriber
{
    /// <summary>
    /// Subscribes to events of a specific type
    /// </summary>
    Task SubscribeAsync<TEvent>(Func<TEvent, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    /// <summary>
    /// Unsubscribes from events
    /// </summary>
    Task UnsubscribeAsync<TEvent>(CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}
