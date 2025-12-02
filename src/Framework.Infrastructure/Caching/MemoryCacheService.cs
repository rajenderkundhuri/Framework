using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Framework.Application.Caching;
using Framework.Domain.MultiTenancy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Caching;

/// <summary>
/// In-memory cache implementation
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly CacheSettings _settings;
    private readonly ITenantContext? _tenantContext;
    private readonly ConcurrentDictionary<string, bool> _keys = new();

    public MemoryCacheService(
        IMemoryCache cache,
        IOptions<CacheSettings> settings,
        ITenantContext? tenantContext = null)
    {
        _cache = cache;
        _settings = settings.Value;
        _tenantContext = tenantContext;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsEnabled)
            return Task.FromResult<T?>(default);

        var fullKey = BuildKey(key);

        if (_cache.TryGetValue(fullKey, out T? value))
        {
            return Task.FromResult(value);
        }

        return Task.FromResult<T?>(default);
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsEnabled)
            return await factory(cancellationToken);

        var fullKey = BuildKey(key);

        if (_cache.TryGetValue(fullKey, out T? value))
        {
            return value;
        }

        value = await factory(cancellationToken);

        if (value != null)
        {
            await SetAsync(key, value, options, cancellationToken);
        }

        return value;
    }

    public Task SetAsync<T>(string key, T value, CacheOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsEnabled)
            return Task.CompletedTask;

        var fullKey = BuildKey(key);
        var cacheOptions = CreateMemoryCacheOptions(options);

        _cache.Set(fullKey, value, cacheOptions);
        _keys.TryAdd(fullKey, true);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = BuildKey(key);
        _cache.Remove(fullKey);
        _keys.TryRemove(fullKey, out _);

        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var fullPattern = BuildKey(pattern);
        var regex = new Regex(
            "^" + Regex.Escape(fullPattern).Replace("\\*", ".*") + "$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        var keysToRemove = _keys.Keys.Where(k => regex.IsMatch(k)).ToList();

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsEnabled)
            return Task.FromResult(false);

        var fullKey = BuildKey(key);
        return Task.FromResult(_cache.TryGetValue(fullKey, out _));
    }

    public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        // Memory cache doesn't support refresh without getting the value
        // This is a no-op for memory cache with sliding expiration
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        foreach (var key in _keys.Keys)
        {
            _cache.Remove(key);
        }
        _keys.Clear();

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

    private MemoryCacheEntryOptions CreateMemoryCacheOptions(CacheOptions? options)
    {
        var cacheOptions = new MemoryCacheEntryOptions();

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

        cacheOptions.Priority = options?.Priority switch
        {
            CachePriority.Low => CacheItemPriority.Low,
            CachePriority.Normal => CacheItemPriority.Normal,
            CachePriority.High => CacheItemPriority.High,
            CachePriority.NeverRemove => CacheItemPriority.NeverRemove,
            _ => CacheItemPriority.Normal
        };

        return cacheOptions;
    }
}
