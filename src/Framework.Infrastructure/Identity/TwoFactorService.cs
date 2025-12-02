using System.Security.Cryptography;
using System.Text;
using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Framework.Domain.Identity;
using Framework.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using QRCoder;

namespace Framework.Infrastructure.Identity;

/// <summary>
/// Implementation of the two-factor authentication service
/// </summary>
public class TwoFactorService : ITwoFactorService
{
    private readonly ApplicationDbContext _context;
    private const int RecoveryCodeCount = 10;
    private const string Issuer = "Framework";

    public TwoFactorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TwoFactorStatusResponse> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var authenticator = await _context.TwoFactorAuthenticators
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        var recoveryCodeCount = await _context.TwoFactorRecoveryCodes
            .CountAsync(r => r.UserId == userId && !r.IsUsed, cancellationToken);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return new TwoFactorStatusResponse
        {
            IsEnabled = user?.TwoFactorEnabled ?? false,
            HasAuthenticator = authenticator != null,
            AuthenticatorConfirmed = authenticator?.IsConfirmed ?? false,
            RecoveryCodesRemaining = recoveryCodeCount,
            EnabledAt = authenticator?.ConfirmedAt
        };
    }

    public async Task<Result<TwoFactorSetupResponse>> SetupAuthenticatorAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            return Result<TwoFactorSetupResponse>.Failure("User not found");
        }

        // Generate a new secret key
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secretKey);

        // Check for existing authenticator
        var existingAuth = await _context.TwoFactorAuthenticators
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (existingAuth != null)
        {
            existingAuth.Reset(base32Secret);
        }
        else
        {
            var authenticator = new TwoFactorAuthenticator(Guid.NewGuid(), userId, base32Secret);
            _context.TwoFactorAuthenticators.Add(authenticator);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Generate authenticator URI
        var authenticatorUri = GenerateAuthenticatorUri(user.Email ?? user.UserName ?? "User", base32Secret);

        // Generate QR code
        var qrCodeBase64 = GenerateQrCode(authenticatorUri);

        return Result<TwoFactorSetupResponse>.Success(new TwoFactorSetupResponse
        {
            SharedKey = FormatKey(base32Secret),
            AuthenticatorUri = authenticatorUri,
            QrCodeBase64 = qrCodeBase64
        });
    }

    public async Task<Result<TwoFactorConfirmResponse>> ConfirmAuthenticatorAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var authenticator = await _context.TwoFactorAuthenticators
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (authenticator == null)
        {
            return Result<TwoFactorConfirmResponse>.Failure("Authenticator not set up");
        }

        if (authenticator.IsConfirmed)
        {
            return Result<TwoFactorConfirmResponse>.Failure("Authenticator already confirmed");
        }

        // Validate the code
        if (!ValidateTotpCode(authenticator.SecretKey, code))
        {
            return Result<TwoFactorConfirmResponse>.Failure("Invalid verification code");
        }

        // Confirm the authenticator
        authenticator.Confirm();

        // Enable 2FA on user
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user != null)
        {
            user.EnableTwoFactor();
        }

        // Generate recovery codes
        var recoveryCodes = await GenerateAndSaveRecoveryCodesAsync(userId, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<TwoFactorConfirmResponse>.Success(new TwoFactorConfirmResponse
        {
            RecoveryCodes = recoveryCodes
        });
    }

    public async Task<Result> DisableAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var authenticator = await _context.TwoFactorAuthenticators
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (authenticator == null || !authenticator.IsConfirmed)
        {
            return Result.Failure("Two-factor authentication is not enabled");
        }

        // Validate the code
        if (!ValidateTotpCode(authenticator.SecretKey, code))
        {
            return Result.Failure("Invalid verification code");
        }

        // Remove authenticator
        _context.TwoFactorAuthenticators.Remove(authenticator);

        // Remove recovery codes
        var recoveryCodes = await _context.TwoFactorRecoveryCodes
            .Where(r => r.UserId == userId)
            .ToListAsync(cancellationToken);
        _context.TwoFactorRecoveryCodes.RemoveRange(recoveryCodes);

        // Disable 2FA on user
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user != null)
        {
            user.DisableTwoFactor();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ValidateCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var authenticator = await _context.TwoFactorAuthenticators
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsConfirmed, cancellationToken);

        if (authenticator == null)
        {
            return Result.Failure("Two-factor authentication is not enabled");
        }

        if (!ValidateTotpCode(authenticator.SecretKey, code))
        {
            return Result.Failure("Invalid verification code");
        }

        return Result.Success();
    }

    public async Task<Result<RecoveryCodesResponse>> GenerateRecoveryCodesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var authenticator = await _context.TwoFactorAuthenticators
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsConfirmed, cancellationToken);

        if (authenticator == null)
        {
            return Result<RecoveryCodesResponse>.Failure("Two-factor authentication is not enabled");
        }

        // Remove existing recovery codes
        var existingCodes = await _context.TwoFactorRecoveryCodes
            .Where(r => r.UserId == userId)
            .ToListAsync(cancellationToken);
        _context.TwoFactorRecoveryCodes.RemoveRange(existingCodes);

        // Generate new codes
        var recoveryCodes = await GenerateAndSaveRecoveryCodesAsync(userId, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<RecoveryCodesResponse>.Success(new RecoveryCodesResponse
        {
            RecoveryCodes = recoveryCodes
        });
    }

    public async Task<int> GetRecoveryCodeCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.TwoFactorRecoveryCodes
            .CountAsync(r => r.UserId == userId && !r.IsUsed, cancellationToken);
    }

    public async Task<Result> ValidateRecoveryCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Replace(" ", "").Replace("-", "").ToUpperInvariant();
        var codeHash = HashCode(normalizedCode);

        var recoveryCode = await _context.TwoFactorRecoveryCodes
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CodeHash == codeHash && !r.IsUsed, cancellationToken);

        if (recoveryCode == null)
        {
            return Result.Failure("Invalid recovery code");
        }

        recoveryCode.MarkAsUsed();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ResetAuthenticatorAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var authenticator = await _context.TwoFactorAuthenticators
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (authenticator != null)
        {
            _context.TwoFactorAuthenticators.Remove(authenticator);
        }

        // Remove recovery codes
        var recoveryCodes = await _context.TwoFactorRecoveryCodes
            .Where(r => r.UserId == userId)
            .ToListAsync(cancellationToken);
        _context.TwoFactorRecoveryCodes.RemoveRange(recoveryCodes);

        // Disable 2FA on user
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user != null)
        {
            user.DisableTwoFactor();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private bool ValidateTotpCode(string secretKey, string code)
    {
        try
        {
            var secretBytes = Base32Encoding.ToBytes(secretKey);
            var totp = new Totp(secretBytes);
            return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<string>> GenerateAndSaveRecoveryCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var codes = new List<string>();

        for (int i = 0; i < RecoveryCodeCount; i++)
        {
            var code = GenerateRecoveryCode();
            codes.Add(code);

            var codeHash = HashCode(code.Replace("-", ""));
            var recoveryCode = new TwoFactorRecoveryCode(Guid.NewGuid(), userId, codeHash);
            _context.TwoFactorRecoveryCodes.Add(recoveryCode);
        }

        return codes;
    }

    private static string GenerateRecoveryCode()
    {
        var bytes = new byte[5];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return BitConverter.ToString(bytes).Replace("-", "").Insert(5, "-");
    }

    private static string HashCode(string code)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code.ToUpperInvariant()));
        return Convert.ToBase64String(hashBytes);
    }

    private static string GenerateAuthenticatorUri(string email, string secretKey)
    {
        return $"otpauth://totp/{Issuer}:{Uri.EscapeDataString(email)}?secret={secretKey}&issuer={Issuer}&digits=6";
    }

    private static string FormatKey(string key)
    {
        // Format key with spaces every 4 characters for easier reading
        var result = new StringBuilder();
        var keyWithoutSpaces = key.Replace(" ", "");

        for (int i = 0; i < keyWithoutSpaces.Length; i++)
        {
            if (i > 0 && i % 4 == 0)
            {
                result.Append(' ');
            }
            result.Append(keyWithoutSpaces[i]);
        }

        return result.ToString();
    }

    private static string? GenerateQrCode(string text)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(10);
            return Convert.ToBase64String(qrCodeBytes);
        }
        catch
        {
            return null;
        }
    }
}
