using Framework.Domain.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Framework.Infrastructure.MultiTenancy;

/// <summary>
/// Database-backed tenant store with caching
/// </summary>
public class TenantStore : ITenantStore
{
    private readonly DbContext _context;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    private const string CacheKeyPrefix = "tenant_";
    private const string AllTenantsCacheKey = "tenants_all";

    public TenantStore(DbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        var cacheKey = $"{CacheKeyPrefix}id_{identifier.ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out Tenant? cachedTenant))
            return cachedTenant;

        var normalizedIdentifier = identifier.ToLowerInvariant();
        var tenant = await _context.Set<Tenant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Identifier == normalizedIdentifier && !t.IsDeleted, cancellationToken);

        if (tenant != null)
        {
            _cache.Set(cacheKey, tenant, _cacheExpiration);
        }

        return tenant;
    }

    public async Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{tenantId}";

        if (_cache.TryGetValue(cacheKey, out Tenant? cachedTenant))
            return cachedTenant;

        var tenant = await _context.Set<Tenant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (tenant != null)
        {
            _cache.Set(cacheKey, tenant, _cacheExpiration);
        }

        return tenant;
    }

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(AllTenantsCacheKey, out IReadOnlyList<Tenant>? cachedTenants))
            return cachedTenants!;

        var tenants = await _context.Set<Tenant>()
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        _cache.Set(AllTenantsCacheKey, (IReadOnlyList<Tenant>)tenants, _cacheExpiration);

        return tenants;
    }

    public void InvalidateCache(Guid? tenantId = null, string? identifier = null)
    {
        if (tenantId.HasValue)
        {
            _cache.Remove($"{CacheKeyPrefix}{tenantId}");
        }

        if (!string.IsNullOrWhiteSpace(identifier))
        {
            _cache.Remove($"{CacheKeyPrefix}id_{identifier.ToLowerInvariant()}");
        }

        _cache.Remove(AllTenantsCacheKey);
    }
}

/// <summary>
/// In-memory tenant store for testing
/// </summary>
public class InMemoryTenantStore : ITenantStore
{
    private readonly List<Tenant> _tenants = new();
    private readonly object _lock = new();

    public void AddTenant(Tenant tenant)
    {
        lock (_lock)
        {
            _tenants.Add(tenant);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _tenants.Clear();
        }
    }

    public Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var tenant = _tenants.FirstOrDefault(t =>
                t.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase) && !t.IsDeleted);
            return Task.FromResult(tenant);
        }
    }

    public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var tenant = _tenants.FirstOrDefault(t => t.Id == tenantId && !t.IsDeleted);
            return Task.FromResult(tenant);
        }
    }

    public Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var tenants = _tenants.Where(t => !t.IsDeleted).OrderBy(t => t.Name).ToList();
            return Task.FromResult<IReadOnlyList<Tenant>>(tenants);
        }
    }
}
