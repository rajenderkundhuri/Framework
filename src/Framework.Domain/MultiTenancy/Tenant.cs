using Framework.Domain.Common.Entities;

namespace Framework.Domain.MultiTenancy;

/// <summary>
/// Represents a tenant in the multi-tenant system
/// </summary>
public class Tenant : FullAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? NormalizedName { get; private set; }
    public string Identifier { get; private set; } = string.Empty;
    public string? ConnectionString { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? ValidUpto { get; private set; }
    public string? AdminEmail { get; private set; }

    private Tenant() { } // EF Core constructor

    public Tenant(string name, string identifier, string? adminEmail = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Tenant identifier cannot be empty", nameof(identifier));

        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Identifier = identifier.ToLowerInvariant();
        AdminEmail = adminEmail;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty", nameof(name));

        Name = name;
        NormalizedName = name.ToUpperInvariant();
    }

    public void SetConnectionString(string? connectionString)
    {
        ConnectionString = connectionString;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void SetValidity(DateTimeOffset? validUpto)
    {
        ValidUpto = validUpto;
    }

    public bool IsValid()
    {
        if (!IsActive) return false;
        if (IsDeleted) return false;
        if (ValidUpto.HasValue && ValidUpto.Value < DateTimeOffset.UtcNow) return false;
        return true;
    }
}
