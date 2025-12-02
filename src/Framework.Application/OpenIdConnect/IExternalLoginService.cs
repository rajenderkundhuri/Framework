using Framework.Application.Common.Models;

namespace Framework.Application.OpenIdConnect;

/// <summary>
/// Service for handling external login/identity provider linking
/// </summary>
public interface IExternalLoginService
{
    /// <summary>
    /// Find user by external login provider and key
    /// </summary>
    Task<ExternalLoginResult?> FindByLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Link external login to existing user
    /// </summary>
    Task<Result> LinkLoginAsync(Guid userId, string provider, string providerKey, string? displayName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlink external login from user
    /// </summary>
    Task<Result> UnlinkLoginAsync(Guid userId, string provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all external logins for user
    /// </summary>
    Task<IEnumerable<ExternalLoginInfo>> GetLoginsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process external login - either find existing user or handle new login
    /// </summary>
    Task<Result<ExternalLoginResult>> ProcessExternalLoginAsync(
        string provider,
        string providerKey,
        string email,
        string? name = null,
        IDictionary<string, string>? claims = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// External login result
/// </summary>
public class ExternalLoginResult
{
    /// <summary>
    /// User ID if found/linked
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// User email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Whether the user was found
    /// </summary>
    public bool UserFound { get; set; }

    /// <summary>
    /// Whether the login is linked to an existing user
    /// </summary>
    public bool IsLinked { get; set; }

    /// <summary>
    /// Provider name
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Provider key (external user ID)
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;
}

/// <summary>
/// External login information
/// </summary>
public class ExternalLoginInfo
{
    /// <summary>
    /// Provider name (e.g., "AzureAD", "Google")
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Provider key (external user ID)
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// When the login was linked
    /// </summary>
    public DateTime LinkedAt { get; set; }
}
