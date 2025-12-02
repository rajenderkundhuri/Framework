namespace Framework.Domain.Common.Entities;

/// <summary>
/// Marker interface for entities
/// </summary>
public interface IEntity
{
}

/// <summary>
/// Generic entity interface with typed key
/// </summary>
/// <typeparam name="TKey">Type of the entity's primary key</typeparam>
public interface IEntity<TKey> : IEntity where TKey : notnull
{
    TKey Id { get; }
}
