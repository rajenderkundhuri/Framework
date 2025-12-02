using System.Reflection;
using System.Text.Json;
using Framework.Application.Auditing;
using Framework.Domain.Auditing;
using Framework.Domain.Common.Entities;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Auditing;

/// <summary>
/// EF Core interceptor for automatic audit logging
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuditSettings _settings;

    public AuditInterceptor(
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        IHttpContextAccessor httpContextAccessor,
        IOptions<AuditSettings> settings)
    {
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
        _settings = settings.Value;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsEnabled || eventData.Context == null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var auditLogs = CreateAuditLogs(eventData.Context);

        if (auditLogs.Any())
        {
            eventData.Context.Set<AuditLog>().AddRange(auditLogs);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private List<AuditLog> CreateAuditLogs(DbContext context)
    {
        var auditLogs = new List<AuditLog>();
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => ShouldAudit(e))
            .ToList();

        foreach (var entry in entries)
        {
            var auditLog = CreateAuditLog(entry);
            if (auditLog != null)
            {
                auditLogs.Add(auditLog);
            }
        }

        return auditLogs;
    }

    private bool ShouldAudit(EntityEntry entry)
    {
        // Skip AuditLog entity itself
        if (entry.Entity is AuditLog)
            return false;

        var entityType = entry.Entity.GetType().Name;

        // Check if entity type is excluded
        if (_settings.ExcludedEntityTypes.Contains(entityType))
            return false;

        // Only audit entities that implement IAuditable or IAuditableEntity
        var implementsAuditable = entry.Entity is IAuditable || entry.Entity is IAuditableEntity;

        return implementsAuditable;
    }

    private AuditLog? CreateAuditLog(EntityEntry entry)
    {
        var action = entry.State switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Modified => DetermineModifiedAction(entry),
            EntityState.Deleted => AuditAction.Delete,
            _ => AuditAction.None
        };

        if (action == AuditAction.None)
            return null;

        if (_settings.ExcludedActions.Contains(action))
            return null;

        var entityType = entry.Entity.GetType().Name;
        var entityId = GetEntityId(entry);

        var auditLog = new AuditLog(
            action,
            entityType,
            entityId,
            _currentUser.UserId,
            _currentUser.UserName,
            _tenantContext.TenantId);

        // Set request info if available
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && _settings.LogRequestInfo)
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            auditLog.SetRequestInfo(ipAddress, userAgent);
        }

        // Set entity changes if enabled
        if (_settings.LogEntityChanges)
        {
            var (oldValues, newValues, affectedColumns) = GetEntityChanges(entry);
            auditLog.SetEntityChanges(oldValues, newValues, affectedColumns);
        }

        return auditLog;
    }

    private AuditAction DetermineModifiedAction(EntityEntry entry)
    {
        // Check if this is a soft delete
        if (entry.Entity is ISoftDelete softDelete)
        {
            var isDeletedProperty = entry.Property(nameof(ISoftDelete.IsDeleted));
            if (isDeletedProperty.IsModified)
            {
                var wasDeleted = (bool?)isDeletedProperty.OriginalValue ?? false;
                var isDeleted = softDelete.IsDeleted;

                if (!wasDeleted && isDeleted)
                    return AuditAction.SoftDelete;

                if (wasDeleted && !isDeleted)
                    return AuditAction.Restore;
            }
        }

        return AuditAction.Update;
    }

    private string? GetEntityId(EntityEntry entry)
    {
        var keyProperties = entry.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .ToList();

        if (!keyProperties.Any())
            return null;

        if (keyProperties.Count == 1)
            return keyProperties[0].CurrentValue?.ToString();

        // Composite key
        var keyValues = keyProperties
            .Select(p => $"{p.Metadata.Name}={p.CurrentValue}")
            .ToArray();

        return string.Join(",", keyValues);
    }

    private (string? oldValues, string? newValues, string? affectedColumns) GetEntityChanges(EntityEntry entry)
    {
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();
        var affectedColumns = new List<string>();

        foreach (var property in entry.Properties)
        {
            // Skip shadow properties and keys
            if (property.Metadata.IsShadowProperty() || property.Metadata.IsPrimaryKey())
                continue;

            var propertyName = property.Metadata.Name;

            // Check for AuditIgnore attribute
            var propertyInfo = entry.Entity.GetType().GetProperty(propertyName);
            if (propertyInfo?.GetCustomAttribute<AuditIgnoreAttribute>() != null)
                continue;

            // Check for AuditMask attribute
            var maskAttribute = propertyInfo?.GetCustomAttribute<AuditMaskAttribute>();

            switch (entry.State)
            {
                case EntityState.Added:
                    var addedValue = maskAttribute != null ? maskAttribute.MaskValue : property.CurrentValue;
                    newValues[propertyName] = addedValue;
                    affectedColumns.Add(propertyName);
                    break;

                case EntityState.Deleted:
                    var deletedValue = maskAttribute != null ? maskAttribute.MaskValue : property.OriginalValue;
                    oldValues[propertyName] = deletedValue;
                    affectedColumns.Add(propertyName);
                    break;

                case EntityState.Modified:
                    if (property.IsModified)
                    {
                        var originalValue = maskAttribute != null ? maskAttribute.MaskValue : property.OriginalValue;
                        var currentValue = maskAttribute != null ? maskAttribute.MaskValue : property.CurrentValue;

                        oldValues[propertyName] = originalValue;
                        newValues[propertyName] = currentValue;
                        affectedColumns.Add(propertyName);
                    }
                    break;
            }
        }

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return (
            oldValues.Any() ? JsonSerializer.Serialize(oldValues, jsonOptions) : null,
            newValues.Any() ? JsonSerializer.Serialize(newValues, jsonOptions) : null,
            affectedColumns.Any() ? string.Join(",", affectedColumns) : null
        );
    }
}
