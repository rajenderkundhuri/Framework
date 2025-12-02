using Framework.Domain.Common.Entities;
using Framework.Domain.Common.Interfaces;

namespace Framework.Domain.Identity;

/// <summary>
/// Application user entity extending IdentityUser with additional properties
/// </summary>
public class ApplicationUser : AuditableEntity, ISoftDelete
{
    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    public ApplicationUser() : base()
    {
    }

    /// <summary>
    /// Creates a new user with specified ID
    /// </summary>
    public ApplicationUser(Guid id) : base(id)
    {
    }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PasswordHash { get; set; }
    public string? SecurityStamp { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }
    public string? UserName { get; set; }
    public string? NormalizedUserName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiryTime { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();

    /// <summary>
    /// Enable two-factor authentication
    /// </summary>
    public void EnableTwoFactor()
    {
        TwoFactorEnabled = true;
    }

    /// <summary>
    /// Disable two-factor authentication
    /// </summary>
    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
    }
}

/// <summary>
/// Join entity for user-role relationship
/// </summary>
public class ApplicationUserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ApplicationRole Role { get; set; } = null!;
}
