namespace Framework.Domain.Common.Interfaces;

/// <summary>
/// Interface for accessing current user information
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets the current user's ID
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the current user's username
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Gets the current user's email
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the current user's tenant ID (for multi-tenant applications)
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Gets whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current user's roles
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Gets a specific claim value
    /// </summary>
    string? GetClaim(string claimType);

    /// <summary>
    /// Gets all claims of a specific type
    /// </summary>
    IEnumerable<string> GetClaims(string claimType);
}
