using Framework.Domain.Common.Entities;

namespace Framework.Domain.MultiTenancy;

/// <summary>
/// Base class for multi-tenant entities
/// </summary>
public abstract class MultiTenantEntity : AuditableEntity, IMultiTenant
{
    public Guid? TenantId { get; protected set; }

    protected MultiTenantEntity() : base() { }

    protected MultiTenantEntity(Guid id) : base(id) { }

    public void SetTenantId(Guid? tenantId)
    {
        TenantId = tenantId;
    }
}

/// <summary>
/// Base class for multi-tenant entities with typed ID
/// </summary>
public abstract class MultiTenantEntity<TId> : AuditableEntity<TId>, IMultiTenant
    where TId : notnull
{
    public Guid? TenantId { get; protected set; }

    protected MultiTenantEntity() : base() { }

    protected MultiTenantEntity(TId id) : base(id) { }

    public void SetTenantId(Guid? tenantId)
    {
        TenantId = tenantId;
    }
}

/// <summary>
/// Base class for multi-tenant entities with soft delete
/// </summary>
public abstract class FullMultiTenantEntity : FullAuditableEntity, IMultiTenant
{
    public Guid? TenantId { get; protected set; }

    protected FullMultiTenantEntity() : base() { }

    protected FullMultiTenantEntity(Guid id) : base(id) { }

    public void SetTenantId(Guid? tenantId)
    {
        TenantId = tenantId;
    }
}

/// <summary>
/// Base class for multi-tenant entities with typed ID and soft delete
/// </summary>
public abstract class FullMultiTenantEntity<TId> : FullAuditableEntity<TId>, IMultiTenant
    where TId : notnull
{
    public Guid? TenantId { get; protected set; }

    protected FullMultiTenantEntity() : base() { }

    protected FullMultiTenantEntity(TId id) : base(id) { }

    public void SetTenantId(Guid? tenantId)
    {
        TenantId = tenantId;
    }
}
