namespace Framework.Domain.Common.Entities;

/// <summary>
/// Base class for all entities with a typed primary key
/// </summary>
/// <typeparam name="TKey">Type of the entity's primary key</typeparam>
public abstract class Entity<TKey> : IEntity<TKey> where TKey : notnull
{
    public virtual TKey Id { get; protected set; } = default!;

    protected Entity()
    {
    }

    protected Entity(TKey id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TKey> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (IsTransient() || other.IsTransient())
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return IsTransient() ? base.GetHashCode() : Id.GetHashCode();
    }

    public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Indicates whether the entity has been persisted
    /// </summary>
    public bool IsTransient()
    {
        return Id.Equals(default(TKey));
    }
}

/// <summary>
/// Base entity with Guid as primary key
/// </summary>
public abstract class Entity : Entity<Guid>
{
    protected Entity() : base()
    {
    }

    protected Entity(Guid id) : base(id)
    {
    }
}
