namespace Framework.Application.Caching;

/// <summary>
/// Configuration settings for caching
/// </summary>
public class CacheSettings
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Whether caching is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Type of cache to use
    /// </summary>
    public CacheType CacheType { get; set; } = CacheType.Memory;

    /// <summary>
    /// Default expiration time in minutes
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Redis connection string (when using distributed cache)
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Redis instance name
    /// </summary>
    public string RedisInstanceName { get; set; } = "Framework:";

    /// <summary>
    /// SQL Server connection string (when using SQL distributed cache)
    /// </summary>
    public string? SqlServerConnectionString { get; set; }

    /// <summary>
    /// SQL Server schema name
    /// </summary>
    public string SqlServerSchemaName { get; set; } = "dbo";

    /// <summary>
    /// SQL Server table name
    /// </summary>
    public string SqlServerTableName { get; set; } = "Cache";

    /// <summary>
    /// Whether to use tenant isolation for cache keys
    /// </summary>
    public bool UseTenantIsolation { get; set; } = true;

    /// <summary>
    /// Key prefix for all cache entries
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Memory cache size limit in megabytes (0 = unlimited)
    /// </summary>
    public int MemoryCacheSizeLimitMB { get; set; } = 100;

    /// <summary>
    /// Compaction percentage when size limit is reached
    /// </summary>
    public double CompactionPercentage { get; set; } = 0.25;
}

/// <summary>
/// Type of cache implementation
/// </summary>
public enum CacheType
{
    /// <summary>
    /// In-memory cache (single server)
    /// </summary>
    Memory = 0,

    /// <summary>
    /// Redis distributed cache
    /// </summary>
    Redis = 1,

    /// <summary>
    /// SQL Server distributed cache
    /// </summary>
    SqlServer = 2
}
