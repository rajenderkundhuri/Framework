using Framework.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Controllers;

/// <summary>
/// Two-factor authentication endpoints
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class TwoFactorController : ApiControllerBase
{
    private readonly ITwoFactorService _twoFactorService;

    public TwoFactorController(ITwoFactorService twoFactorService)
    {
        _twoFactorService = twoFactorService;
    }

    /// <summary>
    /// Get current 2FA status for the authenticated user
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(TwoFactorStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _twoFactorService.GetStatusAsync(userId.Value, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Begin 2FA setup - generates secret and QR code
    /// </summary>
    [HttpPost("setup")]
    [ProducesResponseType(typeof(TwoFactorSetupResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BeginSetup(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _twoFactorService.SetupAuthenticatorAsync(userId.Value, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Confirm 2FA setup with verification code
    /// </summary>
    [HttpPost("setup/confirm")]
    [ProducesResponseType(typeof(TwoFactorConfirmResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmSetup([FromBody] ConfirmSetupRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _twoFactorService.ConfirmAuthenticatorAsync(userId.Value, request.VerificationCode, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Disable 2FA for the authenticated user
    /// </summary>
    [HttpPost("disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Disable([FromBody] DisableTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _twoFactorService.DisableAsync(userId.Value, request.VerificationCode, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Validate a TOTP code (for login flow)
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateCode([FromBody] ValidateTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var result = await _twoFactorService.ValidateCodeAsync(request.UserId, request.Code, cancellationToken);
        return Ok(result.IsSuccess);
    }

    /// <summary>
    /// Generate new recovery codes
    /// </summary>
    [HttpPost("recovery-codes/regenerate")]
    [ProducesResponseType(typeof(RecoveryCodesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegenerateRecoveryCodes(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _twoFactorService.GenerateRecoveryCodesAsync(userId.Value, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Use a recovery code to authenticate
    /// </summary>
    [HttpPost("recovery-codes/use")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> UseRecoveryCode([FromBody] UseRecoveryCodeRequest request, CancellationToken cancellationToken)
    {
        var result = await _twoFactorService.ValidateRecoveryCodeAsync(request.UserId, request.RecoveryCode, cancellationToken);
        return Ok(result.IsSuccess);
    }

    /// <summary>
    /// Get remaining recovery codes count
    /// </summary>
    [HttpGet("recovery-codes/count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecoveryCodesCount(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var count = await _twoFactorService.GetRecoveryCodeCountAsync(userId.Value, cancellationToken);
        return Ok(count);
    }

    /// <summary>
    /// Reset authenticator (for re-setup)
    /// </summary>
    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetAuthenticator(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _twoFactorService.ResetAuthenticatorAsync(userId.Value, cancellationToken);
        return HandleResult(result);
    }
}

/// <summary>
/// Request to confirm 2FA setup
/// </summary>
public record ConfirmSetupRequest
{
    public string VerificationCode { get; init; } = string.Empty;
}

/// <summary>
/// Request to disable 2FA
/// </summary>
public record DisableTwoFactorRequest
{
    public string VerificationCode { get; init; } = string.Empty;
}

/// <summary>
/// Request to validate 2FA code
/// </summary>
public record ValidateTwoFactorRequest
{
    public Guid UserId { get; init; }
    public string Code { get; init; } = string.Empty;
}

/// <summary>
/// Request to use a recovery code
/// </summary>
public record UseRecoveryCodeRequest
{
    public Guid UserId { get; init; }
    public string RecoveryCode { get; init; } = string.Empty;
}
