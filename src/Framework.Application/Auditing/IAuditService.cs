using Framework.Application.Common.Models;
using Framework.Domain.Auditing;

namespace Framework.Application.Auditing;

/// <summary>
/// Service for managing audit logs
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Log an audit entry
    /// </summary>
    Task LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Log a custom action
    /// </summary>
    Task LogActionAsync(
        AuditAction action,
        string entityType,
        string? entityId = null,
        string? additionalInfo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs with filtering and pagination
    /// </summary>
    Task<PagedList<AuditLogDto>> GetLogsAsync(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    Task<IReadOnlyList<AuditLogDto>> GetEntityLogsAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for a specific user
    /// </summary>
    Task<PagedList<AuditLogDto>> GetUserLogsAsync(
        string userId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete old audit logs
    /// </summary>
    Task<int> PurgeLogsAsync(
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Filter for querying audit logs
/// </summary>
public record AuditLogFilter
{
    public Guid? TenantId { get; init; }
    public string? UserId { get; init; }
    public AuditAction? Action { get; init; }
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
    public bool? IsSuccess { get; init; }
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Audit log DTO for API responses
/// </summary>
public record AuditLogDto
{
    public Guid Id { get; init; }
    public Guid? TenantId { get; init; }
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public string? IpAddress { get; init; }
    public AuditAction Action { get; init; }
    public string ActionName => Action.ToString();
    public string EntityType { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? AffectedColumns { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string? AdditionalInfo { get; init; }
    public string? ServiceName { get; init; }
    public string? MethodName { get; init; }
    public int? Duration { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Settings for audit logging
/// </summary>
public class AuditSettings
{
    public const string SectionName = "Audit";

    /// <summary>
    /// Whether audit logging is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether to log entity changes (old/new values)
    /// </summary>
    public bool LogEntityChanges { get; set; } = true;

    /// <summary>
    /// Whether to log request info (IP, User Agent)
    /// </summary>
    public bool LogRequestInfo { get; set; } = true;

    /// <summary>
    /// Days to retain audit logs (0 = forever)
    /// </summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// Entity types to exclude from auditing
    /// </summary>
    public List<string> ExcludedEntityTypes { get; set; } = new();

    /// <summary>
    /// Actions to exclude from auditing
    /// </summary>
    public List<AuditAction> ExcludedActions { get; set; } = new();
}
