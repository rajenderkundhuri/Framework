using Framework.Domain.Common.Entities;

namespace Framework.Domain.Identity;

/// <summary>
/// Application role entity with permissions support
/// </summary>
public class ApplicationRole : AuditableEntity
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(Guid id)
    {
        Id = id;
    }

    public string Name { get; set; } = string.Empty;
    public string? NormalizedName { get; set; }
    public string? Description { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public bool IsDefault { get; set; }
    public bool IsStatic { get; set; }
    public bool IsSystem { get; set; }

    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
    public virtual ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Role-Permission mapping
/// </summary>
public class RolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoleId { get; set; }
    public string Permission { get; set; } = string.Empty;
    public virtual ApplicationRole Role { get; set; } = null!;
}
