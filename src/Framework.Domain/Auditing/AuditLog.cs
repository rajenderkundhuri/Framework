using Framework.Domain.Common.Entities;
using Framework.Domain.MultiTenancy;

namespace Framework.Domain.Auditing;

/// <summary>
/// Represents an audit log entry
/// </summary>
public class AuditLog : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string? UserId { get; private set; }
    public string? UserName { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public AuditAction Action { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? AffectedColumns { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public string? AdditionalInfo { get; private set; }
    public string? ServiceName { get; private set; }
    public string? MethodName { get; private set; }
    public int? Duration { get; private set; }
    public bool IsSuccess { get; private set; } = true;
    public string? ErrorMessage { get; private set; }

    private AuditLog() : base() { }

    public AuditLog(
        AuditAction action,
        string entityType,
        string? entityId = null,
        string? userId = null,
        string? userName = null,
        Guid? tenantId = null)
        : base()
    {
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        UserId = userId;
        UserName = userName;
        TenantId = tenantId;
        Timestamp = DateTimeOffset.UtcNow;
    }

    public void SetRequestInfo(string? ipAddress, string? userAgent)
    {
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public void SetEntityChanges(string? oldValues, string? newValues, string? affectedColumns)
    {
        OldValues = oldValues;
        NewValues = newValues;
        AffectedColumns = affectedColumns;
    }

    public void SetMethodInfo(string? serviceName, string? methodName, int? duration)
    {
        ServiceName = serviceName;
        MethodName = methodName;
        Duration = duration;
    }

    public void SetAdditionalInfo(string? info)
    {
        AdditionalInfo = info;
    }

    public void SetError(string errorMessage)
    {
        IsSuccess = false;
        ErrorMessage = errorMessage;
    }

    public void SetTenantId(Guid? tenantId)
    {
        TenantId = tenantId;
    }
}

/// <summary>
/// Types of audit actions
/// </summary>
public enum AuditAction
{
    None = 0,
    Create = 1,
    Update = 2,
    Delete = 3,
    SoftDelete = 4,
    Restore = 5,
    Login = 10,
    Logout = 11,
    FailedLogin = 12,
    PasswordChange = 13,
    PasswordReset = 14,
    RoleAssigned = 20,
    RoleRemoved = 21,
    PermissionGranted = 22,
    PermissionRevoked = 23,
    Export = 30,
    Import = 31,
    Custom = 100
}

/// <summary>
/// Marker interface for entities that should be audited
/// </summary>
public interface IAuditable
{
}

/// <summary>
/// Attribute to exclude properties from audit logging
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AuditIgnoreAttribute : Attribute
{
}

/// <summary>
/// Attribute to mask sensitive properties in audit logs
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AuditMaskAttribute : Attribute
{
    public string MaskValue { get; }

    public AuditMaskAttribute(string maskValue = "***")
    {
        MaskValue = maskValue;
    }
}
