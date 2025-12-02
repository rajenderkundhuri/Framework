using Framework.Application.Common.Models;
using Framework.Application.Identity.Models;

namespace Framework.Application.Identity.Interfaces;

/// <summary>
/// Identity management service interface
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Authenticate user and return tokens
    /// </summary>
    Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register a new user
    /// </summary>
    Task<Result<Guid>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user profile by ID
    /// </summary>
    Task<Result<UserProfileResponse>> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user profile
    /// </summary>
    Task<Result> UpdateProfileAsync(Guid userId, string firstName, string lastName, string? phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change user password
    /// </summary>
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Request password reset token
    /// </summary>
    Task<Result<string>> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset password using token
    /// </summary>
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirm user email
    /// </summary>
    Task<Result> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user exists
    /// </summary>
    Task<bool> UserExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user roles
    /// </summary>
    Task<Result<IEnumerable<string>>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assign role to user
    /// </summary>
    Task<Result> AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove role from user
    /// </summary>
    Task<Result> RemoveRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user permissions
    /// </summary>
    Task<Result<IEnumerable<string>>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has permission
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout user (revoke refresh token)
    /// </summary>
    Task<Result> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user theme settings
    /// </summary>
    Task<Result> UpdateThemeSettingsAsync(Guid userId, bool isDarkMode, string themeColorName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user theme settings
    /// </summary>
    Task<Result<ThemeSettingsResponse>> GetThemeSettingsAsync(Guid userId, CancellationToken cancellationToken = default);
}
