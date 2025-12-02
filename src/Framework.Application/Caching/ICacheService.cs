namespace Framework.Application.Caching;

/// <summary>
/// Interface for cache operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from cache
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached value or default if not found</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value from cache or creates it using the factory
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Factory to create the value if not in cache</param>
    /// <param name="options">Cache options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached or newly created value</returns>
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in cache
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="options">Cache options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetAsync<T>(string key, T value, CacheOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all values matching a pattern
    /// </summary>
    /// <param name="pattern">Key pattern (supports * wildcard)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if key exists</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the expiration of a cached item
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RefreshAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached items
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for caching
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Absolute expiration time
    /// </summary>
    public DateTimeOffset? AbsoluteExpiration { get; set; }

    /// <summary>
    /// Absolute expiration relative to now
    /// </summary>
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

    /// <summary>
    /// Sliding expiration time
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>
    /// Cache priority
    /// </summary>
    public CachePriority Priority { get; set; } = CachePriority.Normal;

    /// <summary>
    /// Tags for grouping cached items
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Creates default cache options with specified duration
    /// </summary>
    public static CacheOptions Default(TimeSpan? duration = null)
    {
        return new CacheOptions
        {
            AbsoluteExpirationRelativeToNow = duration ?? TimeSpan.FromMinutes(5)
        };
    }

    /// <summary>
    /// Creates cache options with sliding expiration
    /// </summary>
    public static CacheOptions Sliding(TimeSpan duration)
    {
        return new CacheOptions
        {
            SlidingExpiration = duration
        };
    }

    /// <summary>
    /// Creates cache options that never expire
    /// </summary>
    public static CacheOptions NeverExpire()
    {
        return new CacheOptions();
    }
}

/// <summary>
/// Cache priority levels
/// </summary>
public enum CachePriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    NeverRemove = 3
}
