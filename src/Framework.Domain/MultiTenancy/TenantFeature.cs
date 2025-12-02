using Framework.Domain.Common.Entities;

namespace Framework.Domain.MultiTenancy;

/// <summary>
/// Represents a feature flag for a specific tenant
/// </summary>
public class TenantFeature : AuditableEntity
{
    private TenantFeature() : base() { } // EF Core constructor

    public TenantFeature(Guid id, Guid tenantId, string featureName, bool isEnabled = true) : base(id)
    {
        if (string.IsNullOrWhiteSpace(featureName))
            throw new ArgumentException("Feature name cannot be empty", nameof(featureName));

        TenantId = tenantId;
        FeatureName = featureName;
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Feature name
    /// </summary>
    public string FeatureName { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the feature is enabled
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Feature configuration (JSON)
    /// </summary>
    public string? Configuration { get; private set; }

    /// <summary>
    /// When the feature expires (if applicable)
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// Navigation property to tenant
    /// </summary>
    public virtual Tenant Tenant { get; private set; } = null!;

    /// <summary>
    /// Enable the feature
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
    }

    /// <summary>
    /// Disable the feature
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
    }

    /// <summary>
    /// Set feature configuration
    /// </summary>
    public void SetConfiguration(string? configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// Set expiration date
    /// </summary>
    public void SetExpiration(DateTimeOffset? expiresAt)
    {
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Check if feature is currently active
    /// </summary>
    public bool IsActive()
    {
        if (!IsEnabled) return false;
        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTimeOffset.UtcNow) return false;
        return true;
    }
}
