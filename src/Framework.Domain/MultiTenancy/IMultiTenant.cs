namespace Framework.Domain.MultiTenancy;

/// <summary>
/// Interface for entities that belong to a tenant
/// </summary>
public interface IMultiTenant
{
    /// <summary>
    /// The tenant ID this entity belongs to. Null means host-level data.
    /// </summary>
    Guid? TenantId { get; }
}

/// <summary>
/// Interface for accessing current tenant context
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Current tenant ID. Null if host context.
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Current tenant. Null if host context.
    /// </summary>
    Tenant? CurrentTenant { get; }

    /// <summary>
    /// Whether we're in host context (no tenant)
    /// </summary>
    bool IsHost { get; }

    /// <summary>
    /// Whether multi-tenancy is enabled
    /// </summary>
    bool IsEnabled { get; }
}

/// <summary>
/// Interface for tenant resolution strategy
/// </summary>
public interface ITenantResolutionStrategy
{
    /// <summary>
    /// Priority order (lower = higher priority)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Try to resolve tenant identifier from the current context
    /// </summary>
    Task<string?> GetTenantIdentifierAsync();
}

/// <summary>
/// Interface for tenant store (retrieves tenant by identifier)
/// </summary>
public interface ITenantStore
{
    /// <summary>
    /// Get tenant by identifier
    /// </summary>
    Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active tenants
    /// </summary>
    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);
}
