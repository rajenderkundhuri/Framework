using Framework.Application.Auditing;
using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Framework.Domain.Auditing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Controllers;

/// <summary>
/// Audit log endpoints
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class AuditLogsController : ApiControllerBase
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Get paginated list of audit logs
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs([FromQuery] AuditLogFilterRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.SystemAuditLogs))
            return Forbid();

        var filter = new AuditLogFilter
        {
            TenantId = request.TenantId,
            UserId = request.UserId,
            Action = request.Action,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            IsSuccess = request.IsSuccess,
            SearchTerm = request.SearchTerm,
            PageNumber = request.PageNumber > 0 ? request.PageNumber : 1,
            PageSize = request.PageSize > 0 ? request.PageSize : 20
        };

        var result = await _auditService.GetLogsAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    [HttpGet("entity/{entityType}/{entityId}")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEntityLogs(string entityType, string entityId, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.SystemAuditLogs))
            return Forbid();

        var result = await _auditService.GetEntityLogsAsync(entityType, entityId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get audit logs for a specific user
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(PagedList<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserLogs(string userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!HasPermission(Permissions.SystemAuditLogs))
            return Forbid();

        var result = await _auditService.GetUserLogsAsync(userId, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get available audit actions
    /// </summary>
    [HttpGet("actions")]
    [ProducesResponseType(typeof(IEnumerable<AuditActionInfo>), StatusCodes.Status200OK)]
    public IActionResult GetActions()
    {
        var actions = Enum.GetValues<AuditAction>()
            .Select(a => new AuditActionInfo { Value = (int)a, Name = a.ToString() })
            .ToList();

        return Ok(actions);
    }

    /// <summary>
    /// Get distinct entity types from audit logs
    /// </summary>
    [HttpGet("entity-types")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEntityTypes(CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.SystemAuditLogs))
            return Forbid();

        // Get logs to extract entity types (simplified - in production you'd query distinct values)
        var filter = new AuditLogFilter { PageNumber = 1, PageSize = 1000 };
        var result = await _auditService.GetLogsAsync(filter, cancellationToken);
        var entityTypes = result.Items.Select(x => x.EntityType).Distinct().OrderBy(x => x).ToList();

        return Ok(entityTypes);
    }

    /// <summary>
    /// Purge old audit logs
    /// </summary>
    [HttpDelete("purge")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> PurgeLogs([FromQuery] int daysOld = 90, CancellationToken cancellationToken = default)
    {
        if (!HasPermission(Permissions.SystemAuditLogs))
            return Forbid();

        var olderThan = DateTimeOffset.UtcNow.AddDays(-daysOld);
        var count = await _auditService.PurgeLogsAsync(olderThan, cancellationToken);
        return Ok(count);
    }
}

/// <summary>
/// Request model for filtering audit logs
/// </summary>
public record AuditLogFilterRequest
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
/// Audit action info for dropdown
/// </summary>
public record AuditActionInfo
{
    public int Value { get; init; }
    public string Name { get; init; } = string.Empty;
}
