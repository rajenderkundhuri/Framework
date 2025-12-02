namespace Framework.Domain.Common.Entities;

/// <summary>
/// Base class for auditable entities with creation and modification tracking
/// </summary>
/// <typeparam name="TKey">Type of the entity's primary key</typeparam>
public abstract class AuditableEntity<TKey> : Entity<TKey>, IAuditableEntity
    where TKey : notnull
{
    public DateTimeOffset CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedAt { get; private set; }
    public string? LastModifiedBy { get; private set; }

    protected AuditableEntity() : base()
    {
    }

    protected AuditableEntity(TKey id) : base(id)
    {
    }

    public void SetCreated(DateTimeOffset timestamp, string? userId = null)
    {
        CreatedAt = timestamp;
        CreatedBy = userId;
    }

    public void SetModified(DateTimeOffset timestamp, string? userId = null)
    {
        LastModifiedAt = timestamp;
        LastModifiedBy = userId;
    }
}

/// <summary>
/// Auditable entity with Guid as primary key
/// </summary>
public abstract class AuditableEntity : AuditableEntity<Guid>
{
    protected AuditableEntity() : base()
    {
    }

    protected AuditableEntity(Guid id) : base(id)
    {
    }
}

/// <summary>
/// Full auditable entity with soft delete support
/// </summary>
/// <typeparam name="TKey">Type of the entity's primary key</typeparam>
public abstract class FullAuditableEntity<TKey> : AuditableEntity<TKey>, ISoftDelete
    where TKey : notnull
{
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    protected FullAuditableEntity() : base()
    {
    }

    protected FullAuditableEntity(TKey id) : base(id)
    {
    }

    public void SoftDelete(DateTimeOffset timestamp, string? userId = null)
    {
        IsDeleted = true;
        DeletedAt = timestamp;
        DeletedBy = userId;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}

/// <summary>
/// Full auditable entity with Guid as primary key
/// </summary>
public abstract class FullAuditableEntity : FullAuditableEntity<Guid>
{
    protected FullAuditableEntity() : base()
    {
    }

    protected FullAuditableEntity(Guid id) : base(id)
    {
    }
}
