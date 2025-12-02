using Framework.Domain.Common.Entities;

namespace Framework.Domain.Identity;

/// <summary>
/// Two-factor authentication recovery code
/// </summary>
public class TwoFactorRecoveryCode : Entity<Guid>
{
    private TwoFactorRecoveryCode() : base() { }

    public TwoFactorRecoveryCode(Guid id, Guid userId, string codeHash)
        : base(id)
    {
        UserId = userId;
        CodeHash = codeHash;
        CreatedAt = DateTimeOffset.UtcNow;
        IsUsed = false;
    }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Hashed recovery code
    /// </summary>
    public string CodeHash { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the code has been used
    /// </summary>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// When the code was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// When the code was used
    /// </summary>
    public DateTimeOffset? UsedAt { get; private set; }

    /// <summary>
    /// Mark the code as used
    /// </summary>
    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// User's two-factor authenticator configuration
/// </summary>
public class TwoFactorAuthenticator : Entity<Guid>
{
    private TwoFactorAuthenticator() : base() { }

    public TwoFactorAuthenticator(Guid id, Guid userId, string secretKey)
        : base(id)
    {
        UserId = userId;
        SecretKey = secretKey;
        CreatedAt = DateTimeOffset.UtcNow;
        IsConfirmed = false;
    }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// TOTP secret key (encrypted)
    /// </summary>
    public string SecretKey { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the authenticator has been confirmed with a valid code
    /// </summary>
    public bool IsConfirmed { get; private set; }

    /// <summary>
    /// When the authenticator was set up
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// When the authenticator was confirmed
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; private set; }

    /// <summary>
    /// Confirm the authenticator setup
    /// </summary>
    public void Confirm()
    {
        IsConfirmed = true;
        ConfirmedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Reset the authenticator (for re-setup)
    /// </summary>
    public void Reset(string newSecretKey)
    {
        SecretKey = newSecretKey;
        IsConfirmed = false;
        ConfirmedAt = null;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
