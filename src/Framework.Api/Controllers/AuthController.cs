using Framework.Application.Common.Models;
using Framework.Application.Identity.Interfaces;
using Framework.Application.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Controllers;

/// <summary>
/// Authentication and authorization endpoints
/// </summary>
[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public AuthController(IIdentityService identityService, ITokenService tokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// User login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityService.LoginAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// User registration
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityService.RegisterAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _tokenService.RefreshTokenAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _identityService.GetUserProfileAsync(userId.Value, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _identityService.UpdateProfileAsync(
            userId.Value,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Change password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _identityService.ChangePasswordAsync(userId.Value, request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identityService.GeneratePasswordResetTokenAsync(request.Email, cancellationToken);
        // Always return OK to prevent email enumeration
        // In production, send the token via email
        return Ok(new { Message = "If the email exists, a password reset link has been sent." });
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identityService.ResetPasswordAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Logout (revoke refresh token)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _identityService.LogoutAsync(userId.Value, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Update user theme settings
    /// </summary>
    [HttpPut("theme-settings")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateThemeSettings(
        [FromBody] UpdateThemeSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _identityService.UpdateThemeSettingsAsync(
            userId.Value,
            request.IsDarkMode,
            request.ThemeColorName,
            cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Get user theme settings
    /// </summary>
    [HttpGet("theme-settings")]
    [Authorize]
    [ProducesResponseType(typeof(ThemeSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetThemeSettings(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _identityService.GetThemeSettingsAsync(userId.Value, cancellationToken);
        return HandleResult(result);
    }
}

/// <summary>
/// Update profile request
/// </summary>
public record UpdateProfileRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
}

/// <summary>
/// Forgot password request
/// </summary>
public record ForgotPasswordRequest
{
    public string Email { get; init; } = string.Empty;
}
