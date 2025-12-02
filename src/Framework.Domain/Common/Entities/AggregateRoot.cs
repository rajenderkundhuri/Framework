using Framework.Domain.Common.Events;

namespace Framework.Domain.Common.Entities;

/// <summary>
/// Base class for aggregate roots - the entry point to an aggregate
/// </summary>
/// <typeparam name="TKey">Type of the aggregate's primary key</typeparam>
public abstract class AggregateRoot<TKey> : AuditableEntity<TKey>, IHasDomainEvents
    where TKey : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() : base()
    {
    }

    protected AggregateRoot(TKey id) : base(id)
    {
    }

    /// <summary>
    /// Adds a domain event to be dispatched when the aggregate is saved
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Removes a specific domain event
    /// </summary>
    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Clears all domain events (called after dispatch)
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

/// <summary>
/// Aggregate root with Guid as primary key
/// </summary>
public abstract class AggregateRoot : AggregateRoot<Guid>
{
    protected AggregateRoot() : base()
    {
    }

    protected AggregateRoot(Guid id) : base(id)
    {
    }
}
