using Framework.Application.Common.Models;

namespace Framework.Application.MultiTenancy;

/// <summary>
/// Tenant management service interface
/// </summary>
public interface ITenantManagementService
{
    // Tenant CRUD
    Task<Result<PagedList<TenantListResponse>>> GetTenantsAsync(TenantListRequest request, CancellationToken cancellationToken = default);
    Task<Result<TenantDetailResponse>> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Result<TenantDetailResponse>> GetTenantByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Result> ActivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Result> DeactivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    // Feature Management
    Task<Result<IEnumerable<TenantFeatureResponse>>> GetTenantFeaturesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Result> EnableFeatureAsync(Guid tenantId, string featureName, string? configuration = null, CancellationToken cancellationToken = default);
    Task<Result> DisableFeatureAsync(Guid tenantId, string featureName, CancellationToken cancellationToken = default);
    Task<Result> SetFeatureConfigurationAsync(Guid tenantId, string featureName, string? configuration, CancellationToken cancellationToken = default);
    Task<Result<bool>> IsFeatureEnabledAsync(Guid tenantId, string featureName, CancellationToken cancellationToken = default);
    Task<Result> SetFeaturesAsync(Guid tenantId, IEnumerable<SetFeatureRequest> features, CancellationToken cancellationToken = default);

    // Tenant Statistics
    Task<Result<TenantStatisticsResponse>> GetTenantStatisticsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Result<DashboardStatisticsResponse>> GetDashboardStatisticsAsync(CancellationToken cancellationToken = default);
}

#region Request DTOs

/// <summary>
/// Tenant list request
/// </summary>
public class TenantListRequest
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// Create tenant request
/// </summary>
public class CreateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? AdminEmail { get; set; }
    public string? ConnectionString { get; set; }
    public DateTimeOffset? ValidUpto { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SetFeatureRequest> Features { get; set; } = new();
}

/// <summary>
/// Update tenant request
/// </summary>
public class UpdateTenantRequest
{
    public string? Name { get; set; }
    public string? AdminEmail { get; set; }
    public string? ConnectionString { get; set; }
    public DateTimeOffset? ValidUpto { get; set; }
}

/// <summary>
/// Set feature request
/// </summary>
public class SetFeatureRequest
{
    public string FeatureName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? Configuration { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// Tenant list response
/// </summary>
public class TenantListResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? AdminEmail { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? ValidUpto { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int UserCount { get; set; }
    public int EnabledFeatureCount { get; set; }
}

/// <summary>
/// Tenant detail response
/// </summary>
public class TenantDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? AdminEmail { get; set; }
    public string? ConnectionString { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? ValidUpto { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public bool IsValid { get; set; }
    public List<TenantFeatureResponse> Features { get; set; } = new();
}

/// <summary>
/// Tenant feature response
/// </summary>
public class TenantFeatureResponse
{
    public Guid Id { get; set; }
    public string FeatureName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public string? Configuration { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Tenant statistics response
/// </summary>
public class TenantStatisticsResponse
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalFeatures { get; set; }
    public int EnabledFeatures { get; set; }
    public DateTimeOffset? LastActivity { get; set; }
}

/// <summary>
/// Dashboard statistics response
/// </summary>
public class DashboardStatisticsResponse
{
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalRoles { get; set; }
    public List<TenantSummary> RecentTenants { get; set; } = new();
    public List<UserSummary> RecentUsers { get; set; } = new();
}

/// <summary>
/// Tenant summary for dashboard
/// </summary>
public class TenantSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// User summary for dashboard
/// </summary>
public class UserSummary
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

#endregion
