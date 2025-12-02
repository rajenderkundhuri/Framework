using Framework.Application.Common.Models;

namespace Framework.Application.Identity;

/// <summary>
/// Service for two-factor authentication management
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Gets 2FA status for a user
    /// </summary>
    Task<TwoFactorStatusResponse> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables 2FA for a user and generates secret key
    /// </summary>
    Task<Result<TwoFactorSetupResponse>> SetupAuthenticatorAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies and confirms authenticator setup with a TOTP code
    /// </summary>
    Task<Result<TwoFactorConfirmResponse>> ConfirmAuthenticatorAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables 2FA for a user
    /// </summary>
    Task<Result> DisableAsync(Guid userId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a TOTP code
    /// </summary>
    Task<Result> ValidateCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates new recovery codes
    /// </summary>
    Task<Result<RecoveryCodesResponse>> GenerateRecoveryCodesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets remaining recovery code count
    /// </summary>
    Task<int> GetRecoveryCodeCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a recovery code
    /// </summary>
    Task<Result> ValidateRecoveryCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets authenticator (for re-setup)
    /// </summary>
    Task<Result> ResetAuthenticatorAsync(Guid userId, CancellationToken cancellationToken = default);
}

#region Response DTOs

/// <summary>
/// Two-factor status response
/// </summary>
public class TwoFactorStatusResponse
{
    public bool IsEnabled { get; set; }
    public bool HasAuthenticator { get; set; }
    public bool AuthenticatorConfirmed { get; set; }
    public int RecoveryCodesRemaining { get; set; }
    public DateTimeOffset? EnabledAt { get; set; }
}

/// <summary>
/// Two-factor setup response
/// </summary>
public class TwoFactorSetupResponse
{
    /// <summary>
    /// Shared secret key (base32 encoded)
    /// </summary>
    public string SharedKey { get; set; } = string.Empty;

    /// <summary>
    /// OTP Auth URI for QR code generation
    /// </summary>
    public string AuthenticatorUri { get; set; } = string.Empty;

    /// <summary>
    /// QR code as base64 PNG image
    /// </summary>
    public string? QrCodeBase64 { get; set; }
}

/// <summary>
/// Two-factor confirm response
/// </summary>
public class TwoFactorConfirmResponse
{
    /// <summary>
    /// Recovery codes generated during setup
    /// </summary>
    public List<string> RecoveryCodes { get; set; } = new();
}

/// <summary>
/// Recovery codes response
/// </summary>
public class RecoveryCodesResponse
{
    /// <summary>
    /// Newly generated recovery codes
    /// </summary>
    public List<string> RecoveryCodes { get; set; } = new();
}

#endregion
