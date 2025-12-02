using System.Text.Json;
using Framework.Application.Auditing;
using Framework.Application.Common.Models;
using Framework.Domain.Auditing;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Auditing;

/// <summary>
/// Implementation of audit service
/// </summary>
public class AuditService : IAuditService
{
    private readonly DbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuditSettings _settings;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        DbContext context,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        IHttpContextAccessor httpContextAccessor,
        IOptions<AuditSettings> settings,
        ILogger<AuditService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsEnabled)
            return;

        if (_settings.ExcludedEntityTypes.Contains(auditLog.EntityType))
            return;

        if (_settings.ExcludedActions.Contains(auditLog.Action))
            return;

        try
        {
            // Set tenant context if not already set
            if (auditLog.TenantId == null && _tenantContext.TenantId != null)
            {
                auditLog.SetTenantId(_tenantContext.TenantId);
            }

            // Set request info if enabled
            if (_settings.LogRequestInfo)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                    var userAgent = httpContext.Request.Headers.UserAgent.ToString();
                    auditLog.SetRequestInfo(ipAddress, userAgent);
                }
            }

            _context.Set<AuditLog>().Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save audit log for {EntityType} {Action}",
                auditLog.EntityType, auditLog.Action);
        }
    }

    public async Task LogActionAsync(
        AuditAction action,
        string entityType,
        string? entityId = null,
        string? additionalInfo = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog(
            action,
            entityType,
            entityId,
            _currentUser.UserId,
            _currentUser.UserName,
            _tenantContext.TenantId);

        if (!string.IsNullOrEmpty(additionalInfo))
        {
            auditLog.SetAdditionalInfo(additionalInfo);
        }

        await LogAsync(auditLog, cancellationToken);
    }

    public async Task<PagedList<AuditLogDto>> GetLogsAsync(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<AuditLog>().AsNoTracking();

        // Apply filters
        if (filter.TenantId.HasValue)
            query = query.Where(x => x.TenantId == filter.TenantId);

        if (!string.IsNullOrEmpty(filter.UserId))
            query = query.Where(x => x.UserId == filter.UserId);

        if (filter.Action.HasValue)
            query = query.Where(x => x.Action == filter.Action);

        if (!string.IsNullOrEmpty(filter.EntityType))
            query = query.Where(x => x.EntityType == filter.EntityType);

        if (!string.IsNullOrEmpty(filter.EntityId))
            query = query.Where(x => x.EntityId == filter.EntityId);

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.Timestamp >= filter.FromDate);

        if (filter.ToDate.HasValue)
            query = query.Where(x => x.Timestamp <= filter.ToDate);

        if (filter.IsSuccess.HasValue)
            query = query.Where(x => x.IsSuccess == filter.IsSuccess);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(x =>
                (x.UserName != null && x.UserName.ToLower().Contains(searchTerm)) ||
                (x.EntityType != null && x.EntityType.ToLower().Contains(searchTerm)) ||
                (x.AdditionalInfo != null && x.AdditionalInfo.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Timestamp)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => MapToDto(x))
            .ToListAsync(cancellationToken);

        return new PagedList<AuditLogDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetEntityLogsAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        var logs = await _context.Set<AuditLog>()
            .AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .OrderByDescending(x => x.Timestamp)
            .Take(100) // Limit to last 100 entries
            .Select(x => MapToDto(x))
            .ToListAsync(cancellationToken);

        return logs;
    }

    public async Task<PagedList<AuditLogDto>> GetUserLogsAsync(
        string userId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<AuditLog>()
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x))
            .ToListAsync(cancellationToken);

        return new PagedList<AuditLogDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<int> PurgeLogsAsync(
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default)
    {
        var count = await _context.Set<AuditLog>()
            .Where(x => x.Timestamp < olderThan)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation("Purged {Count} audit logs older than {Date}", count, olderThan);

        return count;
    }

    private static AuditLogDto MapToDto(AuditLog log) => new()
    {
        Id = log.Id,
        TenantId = log.TenantId,
        UserId = log.UserId,
        UserName = log.UserName,
        IpAddress = log.IpAddress,
        Action = log.Action,
        EntityType = log.EntityType,
        EntityId = log.EntityId,
        OldValues = log.OldValues,
        NewValues = log.NewValues,
        AffectedColumns = log.AffectedColumns,
        Timestamp = log.Timestamp,
        AdditionalInfo = log.AdditionalInfo,
        ServiceName = log.ServiceName,
        MethodName = log.MethodName,
        Duration = log.Duration,
        IsSuccess = log.IsSuccess,
        ErrorMessage = log.ErrorMessage
    };
}
