using System.Linq.Expressions;
using Framework.Domain.Common.Entities;
using Framework.Domain.Common.Specifications;

namespace Framework.Domain.Common.Interfaces;

/// <summary>
/// Read-only repository interface for querying entities
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
public interface IReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    /// <summary>
    /// Gets an entity by its primary key
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities matching the given predicate
    /// </summary>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching the predicate or null
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the predicate
    /// </summary>
    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the predicate
    /// </summary>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities matching the specification
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetBySpecAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching the specification or null
    /// </summary>
    Task<TEntity?> FirstOrDefaultBySpecAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Full repository interface with write operations
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
public interface IRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    /// <summary>
    /// Adds a new entity
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities
    /// </summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// Updates multiple entities
    /// </summary>
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Removes an entity
    /// </summary>
    void Remove(TEntity entity);

    /// <summary>
    /// Removes multiple entities
    /// </summary>
    void RemoveRange(IEnumerable<TEntity> entities);
}

/// <summary>
/// Repository interface with Guid as primary key
/// </summary>
public interface IRepository<TEntity> : IRepository<TEntity, Guid>
    where TEntity : class, IEntity<Guid>
{
}
