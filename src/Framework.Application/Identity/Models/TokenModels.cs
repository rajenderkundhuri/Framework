namespace Framework.Application.Identity.Models;

/// <summary>
/// JWT token response
/// </summary>
public record TokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";
}

/// <summary>
/// Login request
/// </summary>
public record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Refresh token request
/// </summary>
public record RefreshTokenRequest
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>
/// Registration request
/// </summary>
public record RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
}

/// <summary>
/// Change password request
/// </summary>
public record ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>
/// Reset password request
/// </summary>
public record ResetPasswordRequest
{
    public string Email { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>
/// User profile response
/// </summary>
public record UserProfileResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public IEnumerable<string> Roles { get; init; } = Enumerable.Empty<string>();
    public IEnumerable<string> Permissions { get; init; } = Enumerable.Empty<string>();
    public ThemeSettingsResponse ThemeSettings { get; init; } = new();
}

/// <summary>
/// Theme settings response
/// </summary>
public record ThemeSettingsResponse
{
    public bool IsDarkMode { get; init; }
    public string ThemeColorName { get; init; } = "Default";
}

/// <summary>
/// Update theme settings request
/// </summary>
public record UpdateThemeSettingsRequest
{
    public bool IsDarkMode { get; init; }
    public string ThemeColorName { get; init; } = "Default";
}
