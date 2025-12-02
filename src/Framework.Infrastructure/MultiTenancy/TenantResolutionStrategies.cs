using System.Security.Claims;
using Framework.Application.MultiTenancy;
using Framework.Domain.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.MultiTenancy;

/// <summary>
/// Resolves tenant from HTTP header
/// </summary>
public class HeaderTenantResolutionStrategy : ITenantResolutionStrategy
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MultiTenancySettings _settings;

    public int Priority => 10;

    public HeaderTenantResolutionStrategy(
        IHttpContextAccessor httpContextAccessor,
        IOptions<MultiTenancySettings> settings)
    {
        _httpContextAccessor = httpContextAccessor;
        _settings = settings.Value;
    }

    public Task<string?> GetTenantIdentifierAsync()
    {
        if (!_settings.Resolution.UseHeader)
            return Task.FromResult<string?>(null);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return Task.FromResult<string?>(null);

        var tenantHeader = httpContext.Request.Headers[_settings.TenantHeader].FirstOrDefault();
        return Task.FromResult(tenantHeader);
    }
}

/// <summary>
/// Resolves tenant from query string parameter
/// </summary>
public class QueryStringTenantResolutionStrategy : ITenantResolutionStrategy
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MultiTenancySettings _settings;

    public int Priority => 20;

    public QueryStringTenantResolutionStrategy(
        IHttpContextAccessor httpContextAccessor,
        IOptions<MultiTenancySettings> settings)
    {
        _httpContextAccessor = httpContextAccessor;
        _settings = settings.Value;
    }

    public Task<string?> GetTenantIdentifierAsync()
    {
        if (!_settings.Resolution.UseQueryString)
            return Task.FromResult<string?>(null);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return Task.FromResult<string?>(null);

        var tenantParam = httpContext.Request.Query[_settings.TenantQueryParam].FirstOrDefault();
        return Task.FromResult(tenantParam);
    }
}

/// <summary>
/// Resolves tenant from subdomain (e.g., tenant1.example.com)
/// </summary>
public class SubdomainTenantResolutionStrategy : ITenantResolutionStrategy
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MultiTenancySettings _settings;

    public int Priority => 5;

    public SubdomainTenantResolutionStrategy(
        IHttpContextAccessor httpContextAccessor,
        IOptions<MultiTenancySettings> settings)
    {
        _httpContextAccessor = httpContextAccessor;
        _settings = settings.Value;
    }

    public Task<string?> GetTenantIdentifierAsync()
    {
        if (!_settings.Resolution.UseSubdomain)
            return Task.FromResult<string?>(null);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return Task.FromResult<string?>(null);

        var host = httpContext.Request.Host.Host;
        if (string.IsNullOrEmpty(host))
            return Task.FromResult<string?>(null);

        // Extract subdomain (first part before first dot)
        var parts = host.Split('.');
        if (parts.Length < 3) // Needs at least subdomain.domain.tld
            return Task.FromResult<string?>(null);

        var subdomain = parts[0];

        // Skip common non-tenant subdomains
        if (subdomain.Equals("www", StringComparison.OrdinalIgnoreCase) ||
            subdomain.Equals("api", StringComparison.OrdinalIgnoreCase) ||
            subdomain.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(subdomain);
    }
}

/// <summary>
/// Resolves tenant from route parameter
/// </summary>
public class RouteTenantResolutionStrategy : ITenantResolutionStrategy
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MultiTenancySettings _settings;

    public int Priority => 15;

    public RouteTenantResolutionStrategy(
        IHttpContextAccessor httpContextAccessor,
        IOptions<MultiTenancySettings> settings)
    {
        _httpContextAccessor = httpContextAccessor;
        _settings = settings.Value;
    }

    public Task<string?> GetTenantIdentifierAsync()
    {
        if (!_settings.Resolution.UseRoute)
            return Task.FromResult<string?>(null);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return Task.FromResult<string?>(null);

        // Try to get tenant from route values
        if (httpContext.Request.RouteValues.TryGetValue("tenant", out var tenantValue))
        {
            return Task.FromResult(tenantValue?.ToString());
        }

        return Task.FromResult<string?>(null);
    }
}

/// <summary>
/// Resolves tenant from user claims (for authenticated users)
/// </summary>
public class ClaimTenantResolutionStrategy : ITenantResolutionStrategy
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MultiTenancySettings _settings;

    public int Priority => 30;

    public ClaimTenantResolutionStrategy(
        IHttpContextAccessor httpContextAccessor,
        IOptions<MultiTenancySettings> settings)
    {
        _httpContextAccessor = httpContextAccessor;
        _settings = settings.Value;
    }

    public Task<string?> GetTenantIdentifierAsync()
    {
        if (!_settings.Resolution.UseClaim)
            return Task.FromResult<string?>(null);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated != true)
            return Task.FromResult<string?>(null);

        var tenantClaim = httpContext.User.FindFirst(_settings.TenantClaimType)?.Value;
        return Task.FromResult(tenantClaim);
    }
}
