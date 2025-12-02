namespace Framework.Domain.Common.Events;

/// <summary>
/// Base class for all domain events
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Domain event that includes the entity type and ID that raised it
/// </summary>
/// <typeparam name="TEntityId">Type of the entity's ID</typeparam>
public abstract record EntityDomainEvent<TEntityId>(TEntityId EntityId) : DomainEvent
    where TEntityId : notnull;
