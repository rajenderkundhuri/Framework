using Framework.Application.Events;
using Framework.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Events;

/// <summary>
/// In-memory event dispatcher using DI to resolve handlers
/// </summary>
public class InMemoryEventDispatcher : IEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryEventDispatcher> _logger;
    private readonly EventSettings _settings;

    public InMemoryEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<InMemoryEventDispatcher> logger,
        IOptions<EventSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        if (_settings.LogEvents)
        {
            _logger.LogInformation(
                "Dispatching event {EventType} (ID: {EventId})",
                @event.EventType,
                @event.EventId);
        }

        var handlers = _serviceProvider.GetServices<IEventHandler<TEvent>>().ToList();

        if (handlers.Count == 0)
        {
            _logger.LogDebug("No handlers registered for event {EventType}", @event.EventType);
            return;
        }

        if (_settings.ParallelDispatch)
        {
            await DispatchParallelAsync(@event, handlers, cancellationToken);
        }
        else
        {
            await DispatchSequentialAsync(@event, handlers, cancellationToken);
        }
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var @event in events)
        {
            await DispatchEventAsync(@event, cancellationToken);
        }
    }

    private async Task DispatchEventAsync(IDomainEvent @event, CancellationToken cancellationToken)
    {
        var eventType = @event.GetType();
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        var handlers = _serviceProvider.GetServices(handlerType).ToList();

        foreach (var handler in handlers)
        {
            try
            {
                var method = handlerType.GetMethod("HandleAsync");
                var task = (Task?)method?.Invoke(handler, new object[] { @event, cancellationToken });
                if (task != null)
                {
                    await task;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in handler {HandlerType} for event {EventType}",
                    handler?.GetType().Name, @event.EventType);

                if (_settings.ThrowOnHandlerException)
                {
                    throw;
                }
            }
        }
    }

    private async Task DispatchSequentialAsync<TEvent>(
        TEvent @event,
        List<IEventHandler<TEvent>> handlers,
        CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(@event, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in handler {HandlerType} for event {EventType}",
                    handler.GetType().Name, @event.EventType);

                if (_settings.ThrowOnHandlerException)
                {
                    throw;
                }
            }
        }
    }

    private async Task DispatchParallelAsync<TEvent>(
        TEvent @event,
        List<IEventHandler<TEvent>> handlers,
        CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _settings.MaxParallelism,
            CancellationToken = cancellationToken
        };

        var exceptions = new List<Exception>();

        await Parallel.ForEachAsync(handlers, options, async (handler, ct) =>
        {
            try
            {
                await handler.HandleAsync(@event, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in handler {HandlerType} for event {EventType}",
                    handler.GetType().Name, @event.EventType);

                if (_settings.ThrowOnHandlerException)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }
        });

        if (exceptions.Count > 0)
        {
            throw new AggregateException("One or more event handlers failed", exceptions);
        }
    }
}
