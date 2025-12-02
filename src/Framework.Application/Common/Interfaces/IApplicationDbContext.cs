using Microsoft.EntityFrameworkCore;

namespace Framework.Application.Common.Interfaces;

/// <summary>
/// Interface for the application's database context
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// Gets a DbSet for the specified entity type
    /// </summary>
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    /// <summary>
    /// Saves all changes made in this context to the database
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
