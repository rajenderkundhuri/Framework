using System.Text.Json;
using Framework.Application.Caching;
using Framework.Domain.MultiTenancy;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Caching;

/// <summary>
/// Distributed cache implementation (Redis, SQL Server, etc.)
/// </summary>
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly CacheSettings _settings;
    private readonly ITenantContext? _tenantContext;
    private readonly JsonSerializerOptions _jsonOptions;

    public DistributedCacheService(
        IDistributedCache cache,
        IOptions<CacheSettings> settings,
        ITenantContext? tenantContext = null)
    {
        _cache = cache;
        _settings = settings.Value;
        _tenantContext = tenantContext;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsEnabled)
            return default;

        var fullKey = BuildKey(key);
        var data = await _cache.GetStringAsync(fullKey, cancellationToken);

        if (string.IsNullOrEmpty(data))
            return default;

        return JsonSerializer.Deserialize<T>(data, _jsonOptions);
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsEnabled)
            return await factory(cancellationToken);

        var value = await GetAsync<T>(key, cancellationToken);

        if (value != null)
            return value;

        value = await factory(cancellationToken);

        if (value != null)
        {
            await SetAsync(key, value, options, cancellationToken);
        }

        return value;
    }

    public async Task SetAsync<T>(string key, T value, CacheOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsEnabled)
            return;

        var fullKey = BuildKey(key);
        var data = JsonSerializer.Serialize(value, _jsonOptions);
        var cacheOptions = CreateDistributedCacheOptions(options);

        await _cache.SetStringAsync(fullKey, data, cacheOptions, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = BuildKey(key);
        await _cache.RemoveAsync(fullKey, cancellationToken);
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Pattern-based removal is not supported by IDistributedCache
        // This would need Redis-specific implementation using SCAN and DEL
        // For now, this is a no-op. Use Redis directly for pattern removal.
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsEnabled)
            return false;

        var fullKey = BuildKey(key);
        var data = await _cache.GetAsync(fullKey, cancellationToken);
        return data != null;
    }

    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = BuildKey(key);
        await _cache.RefreshAsync(fullKey, cancellationToken);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        // Clear is not supported by IDistributedCache
        // This would need Redis-specific implementation using FLUSHDB
        return Task.CompletedTask;
    }

    private string BuildKey(string key)
    {
        var prefix = _settings.KeyPrefix;

        if (_settings.UseTenantIsolation && _tenantContext?.TenantId != null)
        {
            prefix = $"{prefix}tenant:{_tenantContext.TenantId}:";
        }

        return $"{prefix}{key}";
    }

    private DistributedCacheEntryOptions CreateDistributedCacheOptions(CacheOptions? options)
    {
        var cacheOptions = new DistributedCacheEntryOptions();

        if (options?.AbsoluteExpiration.HasValue == true)
        {
            cacheOptions.AbsoluteExpiration = options.AbsoluteExpiration;
        }
        else if (options?.AbsoluteExpirationRelativeToNow.HasValue == true)
        {
            cacheOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
        }
        else if (options?.SlidingExpiration.HasValue == true)
        {
            cacheOptions.SlidingExpiration = options.SlidingExpiration;
        }
        else
        {
            cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_settings.DefaultExpirationMinutes);
        }

        return cacheOptions;
    }
}
