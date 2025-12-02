using Framework.Domain.Common.Entities;

namespace Framework.Domain.Identity;

/// <summary>
/// External login provider link
/// </summary>
public class ExternalLogin : Entity<Guid>
{
    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private ExternalLogin() : base()
    {
    }

    /// <summary>
    /// Creates a new external login
    /// </summary>
    public ExternalLogin(Guid id, Guid userId, string provider, string providerKey, string? providerDisplayName = null)
        : base(id)
    {
        UserId = userId;
        Provider = provider;
        ProviderKey = providerKey;
        ProviderDisplayName = providerDisplayName;
        LinkedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Provider name (e.g., "AzureAD", "Google", "Okta")
    /// </summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    /// Provider key (external user ID)
    /// </summary>
    public string ProviderKey { get; private set; } = string.Empty;

    /// <summary>
    /// Display name from provider
    /// </summary>
    public string? ProviderDisplayName { get; private set; }

    /// <summary>
    /// When the login was linked
    /// </summary>
    public DateTime LinkedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual ApplicationUser User { get; private set; } = null!;
}
