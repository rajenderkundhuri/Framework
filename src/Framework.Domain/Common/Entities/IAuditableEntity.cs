namespace Framework.Domain.Common.Entities;

/// <summary>
/// Interface for entities that track creation and modification timestamps
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; }
    string? CreatedBy { get; }
    DateTimeOffset? LastModifiedAt { get; }
    string? LastModifiedBy { get; }
}

/// <summary>
/// Interface for entities that can be soft deleted
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAt { get; }
    string? DeletedBy { get; }
}
