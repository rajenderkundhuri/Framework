namespace Framework.Application.MultiTenancy;

/// <summary>
/// Multi-tenancy configuration settings
/// </summary>
public class MultiTenancySettings
{
    public const string SectionName = "MultiTenancy";

    /// <summary>
    /// Whether multi-tenancy is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Default connection string for tenants without custom connection
    /// </summary>
    public string? DefaultConnectionString { get; set; }

    /// <summary>
    /// Tenant resolution strategies to use
    /// </summary>
    public TenantResolutionSettings Resolution { get; set; } = new();

    /// <summary>
    /// Header name for tenant identification
    /// </summary>
    public string TenantHeader { get; set; } = "X-Tenant-Id";

    /// <summary>
    /// Query string parameter name for tenant identification
    /// </summary>
    public string TenantQueryParam { get; set; } = "tenant";

    /// <summary>
    /// Claim type for tenant identification
    /// </summary>
    public string TenantClaimType { get; set; } = "tenant_id";
}

/// <summary>
/// Tenant resolution strategy settings
/// </summary>
public class TenantResolutionSettings
{
    /// <summary>
    /// Resolve from HTTP header
    /// </summary>
    public bool UseHeader { get; set; } = true;

    /// <summary>
    /// Resolve from query string
    /// </summary>
    public bool UseQueryString { get; set; } = true;

    /// <summary>
    /// Resolve from route/path
    /// </summary>
    public bool UseRoute { get; set; } = false;

    /// <summary>
    /// Resolve from subdomain
    /// </summary>
    public bool UseSubdomain { get; set; } = false;

    /// <summary>
    /// Resolve from user claims
    /// </summary>
    public bool UseClaim { get; set; } = true;
}
