using Framework.Domain.Common.Entities;

namespace Framework.Domain.Identity;

/// <summary>
/// API Key for programmatic access to the API
/// </summary>
public class ApiKey : AuditableEntity
{
    private ApiKey() : base() { }

    public ApiKey(Guid id, Guid userId, string name, string keyHash, string keyPrefix)
        : base(id)
    {
        UserId = userId;
        Name = name;
        KeyHash = keyHash;
        KeyPrefix = keyPrefix;
        IsActive = true;
    }

    /// <summary>
    /// User who owns this API key
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Friendly name for the API key
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Hashed API key (for verification)
    /// </summary>
    public string KeyHash { get; private set; } = string.Empty;

    /// <summary>
    /// First few characters of the key (for identification)
    /// </summary>
    public string KeyPrefix { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the key is active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// When the key expires (null = never)
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// Last time the key was used
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; private set; }

    /// <summary>
    /// IP address of last usage
    /// </summary>
    public string? LastUsedIp { get; private set; }

    /// <summary>
    /// Comma-separated list of allowed scopes/permissions
    /// </summary>
    public string? Scopes { get; private set; }

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual ApplicationUser User { get; private set; } = null!;

    /// <summary>
    /// Check if the key is valid (active and not expired)
    /// </summary>
    public bool IsValid => IsActive && (ExpiresAt == null || ExpiresAt > DateTimeOffset.UtcNow);

    /// <summary>
    /// Revoke the API key
    /// </summary>
    public void Revoke()
    {
        IsActive = false;
    }

    /// <summary>
    /// Update last used information
    /// </summary>
    public void RecordUsage(string? ipAddress = null)
    {
        LastUsedAt = DateTimeOffset.UtcNow;
        LastUsedIp = ipAddress;
    }

    /// <summary>
    /// Set expiration date
    /// </summary>
    public void SetExpiration(DateTimeOffset? expiresAt)
    {
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Set allowed scopes
    /// </summary>
    public void SetScopes(string? scopes)
    {
        Scopes = scopes;
    }

    /// <summary>
    /// Update the key name
    /// </summary>
    public void UpdateName(string name)
    {
        Name = name;
    }
}
