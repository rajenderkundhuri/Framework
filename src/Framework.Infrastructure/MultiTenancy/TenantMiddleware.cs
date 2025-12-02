using Framework.Application.MultiTenancy;
using Framework.Domain.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.MultiTenancy;

/// <summary>
/// Middleware for resolving and setting the current tenant
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        TenantContext tenantContext,
        ITenantStore tenantStore,
        IEnumerable<ITenantResolutionStrategy> strategies,
        IOptions<MultiTenancySettings> settings)
    {
        if (!settings.Value.IsEnabled)
        {
            await _next(context);
            return;
        }

        // Try each strategy in priority order
        var orderedStrategies = strategies.OrderBy(s => s.Priority);
        string? tenantIdentifier = null;

        foreach (var strategy in orderedStrategies)
        {
            tenantIdentifier = await strategy.GetTenantIdentifierAsync();
            if (!string.IsNullOrWhiteSpace(tenantIdentifier))
            {
                _logger.LogDebug("Tenant identifier '{Identifier}' resolved by {Strategy}",
                    tenantIdentifier, strategy.GetType().Name);
                break;
            }
        }

        if (!string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            var tenant = await tenantStore.GetByIdentifierAsync(tenantIdentifier);

            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found for identifier: {Identifier}", tenantIdentifier);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new
                {
                    Error = "Tenant not found",
                    Code = "TENANT_NOT_FOUND"
                });
                return;
            }

            if (!tenant.IsValid())
            {
                _logger.LogWarning("Tenant is not valid: {TenantId} ({Identifier})", tenant.Id, tenant.Identifier);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    Error = "Tenant is not active or has expired",
                    Code = "TENANT_INACTIVE"
                });
                return;
            }

            tenantContext.SetTenant(tenant);
            AsyncLocalTenantContext.SetTenant(tenant);

            _logger.LogDebug("Tenant context set: {TenantId} ({TenantName})", tenant.Id, tenant.Name);
        }
        else
        {
            _logger.LogDebug("No tenant identifier found, using host context");
        }

        try
        {
            await _next(context);
        }
        finally
        {
            tenantContext.Clear();
            AsyncLocalTenantContext.Clear();
        }
    }
}

/// <summary>
/// Extension methods for tenant middleware
/// </summary>
public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseMultiTenancy(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantMiddleware>();
    }
}
