using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Framework.Application.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Controllers;

/// <summary>
/// Tenant management endpoints
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ApiControllerBase
{
    private readonly ITenantManagementService _tenantManagementService;

    public TenantsController(ITenantManagementService tenantManagementService)
    {
        _tenantManagementService = tenantManagementService;
    }

    /// <summary>
    /// Get paginated list of tenants
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<TenantListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenants([FromQuery] TenantListRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.TenantsView))
            return Forbid();

        var result = await _tenantManagementService.GetTenantsAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenant(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.TenantsView))
            return Forbid();

        var result = await _tenantManagementService.GetTenantByIdAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get tenant by identifier
    /// </summary>
    [HttpGet("by-identifier/{identifier}")]
    [ProducesResponseType(typeof(TenantDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantByIdentifier(string identifier, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.TenantsView))
            return Forbid();

        var result = await _tenantManagementService.GetTenantByIdentifierAsync(identifier, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new tenant
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.TenantsCreate))
            return Forbid();

        var result = await _tenantManagementService.CreateTenantAsync(request, cancellationToken);
        return HandleCreatedResult(result, nameof(GetTenant), new { id = result.Value });
    }

    /// <summary>
    /// Update a tenant
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.TenantsEdit))
            return Forbid();

        var result = await _tenantManagementService.UpdateTenantAsync(id, request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a tenant
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTenant(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.TenantsDelete))
            return Forbid();

        var result = await _tenantManagementService.DeleteTenantAsync(id, cancellationToken);
        return HandleDeleteResult(result);
    }

    /// <summary>
    /// Activate a tenant
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateTenant(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.TenantsEdit))
            return Forbid();

        var result = await _tenantManagementService.ActivateTenantAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Deactivate a tenant
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateTenant(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.TenantsEdit))
            return Forbid();

        var result = await _tenantManagementService.DeactivateTenantAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get tenant features
    /// </summary>
    [HttpGet("{id:guid}/features")]
    [ProducesResponseType(typeof(IEnumerable<TenantFeatureResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantFeatures(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.TenantsView))
            return Forbid();

        var result = await _tenantManagementService.GetTenantFeaturesAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Set tenant features (replace all)
    /// </summary>
    [HttpPut("{id:guid}/features")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetTenantFeatures(Guid id, [FromBody] List<SetFeatureRequest> features, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.TenantsManageFeatures))
            return Forbid();

        var result = await _tenantManagementService.SetFeaturesAsync(id, features, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Enable a feature for tenant
    /// </summary>
    [HttpPost("{id:guid}/features/{featureName}/enable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EnableFeature(Guid id, string featureName, [FromBody] FeatureConfigurationRequest? request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.TenantsManageFeatures))
            return Forbid();

        var result = await _tenantManagementService.EnableFeatureAsync(id, featureName, request?.Configuration, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Disable a feature for tenant
    /// </summary>
    [HttpPost("{id:guid}/features/{featureName}/disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisableFeature(Guid id, string featureName, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.TenantsManageFeatures))
            return Forbid();

        var result = await _tenantManagementService.DisableFeatureAsync(id, featureName, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Check if feature is enabled for tenant
    /// </summary>
    [HttpGet("{id:guid}/features/{featureName}/enabled")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IsFeatureEnabled(Guid id, string featureName, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.TenantsView))
            return Forbid();

        var result = await _tenantManagementService.IsFeatureEnabledAsync(id, featureName, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get tenant statistics
    /// </summary>
    [HttpGet("{id:guid}/statistics")]
    [ProducesResponseType(typeof(TenantStatisticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantStatistics(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.TenantsView))
            return Forbid();

        var result = await _tenantManagementService.GetTenantStatisticsAsync(id, cancellationToken);
        return HandleResult(result);
    }
}

/// <summary>
/// Dashboard controller for admin statistics
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ApiControllerBase
{
    private readonly ITenantManagementService _tenantManagementService;

    public DashboardController(ITenantManagementService tenantManagementService)
    {
        _tenantManagementService = tenantManagementService;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(DashboardStatisticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
    {
        var result = await _tenantManagementService.GetDashboardStatisticsAsync(cancellationToken);
        return HandleResult(result);
    }
}

/// <summary>
/// Feature configuration request
/// </summary>
public record FeatureConfigurationRequest
{
    public string? Configuration { get; init; }
}
