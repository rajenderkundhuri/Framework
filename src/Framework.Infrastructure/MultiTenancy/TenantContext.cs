using Framework.Application.MultiTenancy;
using Framework.Domain.MultiTenancy;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.MultiTenancy;

/// <summary>
/// Default implementation of tenant context
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly MultiTenancySettings _settings;

    public Guid? TenantId { get; private set; }
    public Tenant? CurrentTenant { get; private set; }
    public bool IsHost => TenantId == null;
    public bool IsEnabled => _settings.IsEnabled;

    public TenantContext(IOptions<MultiTenancySettings> settings)
    {
        _settings = settings.Value;
    }

    public void SetTenant(Tenant? tenant)
    {
        CurrentTenant = tenant;
        TenantId = tenant?.Id;
    }

    public void SetTenantId(Guid? tenantId)
    {
        TenantId = tenantId;
    }

    public void Clear()
    {
        CurrentTenant = null;
        TenantId = null;
    }
}

/// <summary>
/// Async local tenant context for background operations
/// </summary>
public class AsyncLocalTenantContext : ITenantContext
{
    private static readonly AsyncLocal<TenantHolder> _currentTenant = new();
    private readonly MultiTenancySettings _settings;

    public Guid? TenantId => _currentTenant.Value?.TenantId;
    public Tenant? CurrentTenant => _currentTenant.Value?.Tenant;
    public bool IsHost => TenantId == null;
    public bool IsEnabled => _settings.IsEnabled;

    public AsyncLocalTenantContext(IOptions<MultiTenancySettings> settings)
    {
        _settings = settings.Value;
    }

    public static void SetTenant(Tenant? tenant)
    {
        _currentTenant.Value = new TenantHolder { Tenant = tenant, TenantId = tenant?.Id };
    }

    public static void SetTenantId(Guid? tenantId)
    {
        _currentTenant.Value = new TenantHolder { TenantId = tenantId };
    }

    public static void Clear()
    {
        _currentTenant.Value = null;
    }

    private class TenantHolder
    {
        public Tenant? Tenant { get; set; }
        public Guid? TenantId { get; set; }
    }
}

/// <summary>
/// Scope for temporarily changing tenant context
/// </summary>
public class TenantScope : IDisposable
{
    private readonly Guid? _previousTenantId;
    private readonly Tenant? _previousTenant;
    private readonly TenantContext _context;
    private bool _disposed;

    public TenantScope(TenantContext context, Tenant? tenant)
    {
        _context = context;
        _previousTenantId = context.TenantId;
        _previousTenant = context.CurrentTenant;
        context.SetTenant(tenant);
    }

    public TenantScope(TenantContext context, Guid? tenantId)
    {
        _context = context;
        _previousTenantId = context.TenantId;
        _previousTenant = context.CurrentTenant;
        context.SetTenantId(tenantId);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_previousTenant != null)
        {
            _context.SetTenant(_previousTenant);
        }
        else
        {
            _context.SetTenantId(_previousTenantId);
        }

        _disposed = true;
    }
}
