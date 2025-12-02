namespace Framework.Domain.Events;

/// <summary>
/// Marker interface for domain events
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// Event type name for serialization/logging
    /// </summary>
    string EventType { get; }
}

/// <summary>
/// Base class for domain events
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public virtual string EventType => GetType().Name;
}

/// <summary>
/// Domain event with entity information
/// </summary>
public abstract class EntityDomainEvent<TId> : DomainEvent
{
    /// <summary>
    /// Entity ID that triggered the event
    /// </summary>
    public TId EntityId { get; }

    protected EntityDomainEvent(TId entityId)
    {
        EntityId = entityId;
    }
}

/// <summary>
/// Domain event with tenant context
/// </summary>
public abstract class TenantDomainEvent : DomainEvent
{
    /// <summary>
    /// Tenant ID context
    /// </summary>
    public Guid? TenantId { get; init; }
}

/// <summary>
/// Interface for entities that raise domain events
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Domain events raised by this entity
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all domain events
    /// </summary>
    void ClearDomainEvents();
}

/// <summary>
/// Base class for entities that raise domain events
/// </summary>
public abstract class EntityWithDomainEvents : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
