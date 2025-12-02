using System.Text;

namespace Framework.Application.Caching;

/// <summary>
/// Helper for building cache keys
/// </summary>
public class CacheKeyBuilder
{
    private readonly StringBuilder _builder = new();
    private readonly string _separator;
    private bool _hasSegments;

    public CacheKeyBuilder(string? prefix = null, string separator = ":")
    {
        _separator = separator;
        if (!string.IsNullOrEmpty(prefix))
        {
            _builder.Append(prefix);
            _hasSegments = true;
        }
    }

    /// <summary>
    /// Adds a segment to the cache key
    /// </summary>
    public CacheKeyBuilder Add(string segment)
    {
        if (string.IsNullOrEmpty(segment))
            return this;

        if (_hasSegments)
            _builder.Append(_separator);

        _builder.Append(segment);
        _hasSegments = true;
        return this;
    }

    /// <summary>
    /// Adds a segment with a value
    /// </summary>
    public CacheKeyBuilder Add(string segment, object? value)
    {
        if (value == null)
            return this;

        return Add($"{segment}={value}");
    }

    /// <summary>
    /// Adds tenant isolation segment
    /// </summary>
    public CacheKeyBuilder WithTenant(Guid? tenantId)
    {
        if (tenantId.HasValue)
            return Add("tenant", tenantId.Value);
        return Add("tenant", "host");
    }

    /// <summary>
    /// Adds user isolation segment
    /// </summary>
    public CacheKeyBuilder WithUser(string? userId)
    {
        if (!string.IsNullOrEmpty(userId))
            return Add("user", userId);
        return this;
    }

    /// <summary>
    /// Adds entity type segment
    /// </summary>
    public CacheKeyBuilder ForEntity<T>()
    {
        return Add(typeof(T).Name.ToLowerInvariant());
    }

    /// <summary>
    /// Adds entity type segment with ID
    /// </summary>
    public CacheKeyBuilder ForEntity<T>(object id)
    {
        return ForEntity<T>().Add(id.ToString()!);
    }

    /// <summary>
    /// Builds the cache key
    /// </summary>
    public string Build()
    {
        return _builder.ToString();
    }

    public override string ToString() => Build();

    /// <summary>
    /// Creates a new cache key builder
    /// </summary>
    public static CacheKeyBuilder Create(string? prefix = null) => new(prefix);

    /// <summary>
    /// Creates a cache key for an entity
    /// </summary>
    public static string ForEntity<T>(object id, string? prefix = null)
    {
        return new CacheKeyBuilder(prefix)
            .ForEntity<T>()
            .Add(id.ToString()!)
            .Build();
    }

    /// <summary>
    /// Creates a cache key for a collection
    /// </summary>
    public static string ForCollection<T>(string? prefix = null)
    {
        return new CacheKeyBuilder(prefix)
            .ForEntity<T>()
            .Add("all")
            .Build();
    }

    /// <summary>
    /// Creates a cache key for a query
    /// </summary>
    public static string ForQuery<T>(string queryName, string? prefix = null, params object[] parameters)
    {
        var builder = new CacheKeyBuilder(prefix)
            .ForEntity<T>()
            .Add("query")
            .Add(queryName);

        foreach (var param in parameters)
        {
            builder.Add(param.ToString()!);
        }

        return builder.Build();
    }
}

/// <summary>
/// Predefined cache key patterns
/// </summary>
public static class CacheKeys
{
    public const string TenantPrefix = "tenant";
    public const string UserPrefix = "user";
    public const string ConfigPrefix = "config";
    public const string PermissionPrefix = "permissions";
    public const string RolePrefix = "roles";

    /// <summary>
    /// Creates a tenant-specific cache key
    /// </summary>
    public static string Tenant(Guid tenantId, string key)
        => $"{TenantPrefix}:{tenantId}:{key}";

    /// <summary>
    /// Creates a user-specific cache key
    /// </summary>
    public static string User(string userId, string key)
        => $"{UserPrefix}:{userId}:{key}";

    /// <summary>
    /// Creates a config cache key
    /// </summary>
    public static string Config(string key)
        => $"{ConfigPrefix}:{key}";

    /// <summary>
    /// Creates a permissions cache key for a user
    /// </summary>
    public static string UserPermissions(string userId)
        => $"{PermissionPrefix}:{userId}";

    /// <summary>
    /// Creates a roles cache key for a user
    /// </summary>
    public static string UserRoles(string userId)
        => $"{RolePrefix}:{userId}";

    /// <summary>
    /// Pattern for all tenant cache keys
    /// </summary>
    public static string TenantPattern(Guid tenantId)
        => $"{TenantPrefix}:{tenantId}:*";

    /// <summary>
    /// Pattern for all user cache keys
    /// </summary>
    public static string UserPattern(string userId)
        => $"{UserPrefix}:{userId}:*";
}
