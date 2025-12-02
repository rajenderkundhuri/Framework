using Framework.Application.Common.Models;
using Framework.Application.Localization;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Framework.Infrastructure.Localization;

/// <summary>
/// Database-backed resource management service
/// </summary>
public class ResourceManagementService : IResourceManagementService
{
    private readonly DbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _dateTime;
    private readonly ILogger<ResourceManagementService> _logger;

    public ResourceManagementService(
        DbContext context,
        ICurrentUser currentUser,
        IDateTime dateTime,
        ILogger<ResourceManagementService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _logger = logger;
    }

    #region Resource CRUD

    public async Task<Result<PagedList<ResourceResponse>>> GetResourcesAsync(
        ResourceListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<LocalizationResource>().AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(r =>
                r.Key.ToLower().Contains(search) ||
                r.Value.ToLower().Contains(search) ||
                (r.Description != null && r.Description.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.CultureName))
        {
            query = query.Where(r => r.CultureName == request.CultureName);
        }

        if (!string.IsNullOrWhiteSpace(request.Group))
        {
            query = query.Where(r => r.Group == request.Group);
        }

        if (request.IsSystem.HasValue)
        {
            query = query.Where(r => r.IsSystem == request.IsSystem.Value);
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "value" => request.SortDescending
                ? query.OrderByDescending(r => r.Value)
                : query.OrderBy(r => r.Value),
            "culturename" => request.SortDescending
                ? query.OrderByDescending(r => r.CultureName)
                : query.OrderBy(r => r.CultureName),
            "group" => request.SortDescending
                ? query.OrderByDescending(r => r.Group)
                : query.OrderBy(r => r.Group),
            "createdat" => request.SortDescending
                ? query.OrderByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.CreatedAt),
            _ => request.SortDescending
                ? query.OrderByDescending(r => r.Key)
                : query.OrderBy(r => r.Key)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var resources = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ResourceResponse
            {
                Id = r.Id,
                Key = r.Key,
                CultureName = r.CultureName,
                Value = r.Value,
                Group = r.Group,
                Description = r.Description,
                IsSystem = r.IsSystem,
                CreatedAt = r.CreatedAt,
                LastModifiedAt = r.LastModifiedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedList<ResourceResponse>(resources, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<Result<ResourceResponse>> GetResourceByIdAsync(
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var resource = await _context.Set<LocalizationResource>()
            .FirstOrDefaultAsync(r => r.Id == resourceId, cancellationToken);

        if (resource == null)
            return Result<ResourceResponse>.NotFound("Resource not found");

        return new ResourceResponse
        {
            Id = resource.Id,
            Key = resource.Key,
            CultureName = resource.CultureName,
            Value = resource.Value,
            Group = resource.Group,
            Description = resource.Description,
            IsSystem = resource.IsSystem,
            CreatedAt = resource.CreatedAt,
            LastModifiedAt = resource.LastModifiedAt
        };
    }

    public async Task<Result<ResourceResponse>> GetResourceAsync(
        string key,
        string cultureName,
        CancellationToken cancellationToken = default)
    {
        var resource = await _context.Set<LocalizationResource>()
            .FirstOrDefaultAsync(r => r.Key == key && r.CultureName == cultureName, cancellationToken);

        if (resource == null)
            return Result<ResourceResponse>.NotFound("Resource not found");

        return new ResourceResponse
        {
            Id = resource.Id,
            Key = resource.Key,
            CultureName = resource.CultureName,
            Value = resource.Value,
            Group = resource.Group,
            Description = resource.Description,
            IsSystem = resource.IsSystem,
            CreatedAt = resource.CreatedAt,
            LastModifiedAt = resource.LastModifiedAt
        };
    }

    public async Task<Result<Guid>> CreateResourceAsync(
        CreateResourceRequest request,
        CancellationToken cancellationToken = default)
    {
        // Check for existing resource
        var existing = await _context.Set<LocalizationResource>()
            .FirstOrDefaultAsync(r => r.Key == request.Key && r.CultureName == request.CultureName, cancellationToken);

        if (existing != null)
            return Result<Guid>.Conflict("A resource with this key and culture already exists");

        var resource = new LocalizationResource(request.Key, request.CultureName, request.Value);
        resource.UpdateMetadata(request.Group, request.Description);
        resource.SetCreated(_dateTime.Now, _currentUser.UserId ?? "system");

        _context.Set<LocalizationResource>().Add(resource);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created localization resource {Key} for culture {Culture}",
            request.Key, request.CultureName);

        return resource.Id;
    }

    public async Task<Result> UpdateResourceAsync(
        Guid resourceId,
        UpdateResourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var resource = await _context.Set<LocalizationResource>()
            .FirstOrDefaultAsync(r => r.Id == resourceId, cancellationToken);

        if (resource == null)
            return Result.NotFound("Resource not found");

        if (resource.IsSystem)
            return Result.Forbidden("Cannot modify system resources");

        if (request.Value != null)
            resource.UpdateValue(request.Value);

        resource.UpdateMetadata(
            request.Group ?? resource.Group,
            request.Description ?? resource.Description);

        resource.SetModified(_dateTime.Now, _currentUser.UserId);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated localization resource {ResourceId}", resourceId);

        return Result.Success();
    }

    public async Task<Result> DeleteResourceAsync(
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var resource = await _context.Set<LocalizationResource>()
            .FirstOrDefaultAsync(r => r.Id == resourceId, cancellationToken);

        if (resource == null)
            return Result.NotFound("Resource not found");

        if (resource.IsSystem)
            return Result.Forbidden("Cannot delete system resources");

        _context.Set<LocalizationResource>().Remove(resource);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted localization resource {ResourceId}", resourceId);

        return Result.Success();
    }

    #endregion

    #region Bulk Operations

    public async Task<Result<BulkResourceResult>> ImportResourcesAsync(
        IEnumerable<ImportResourceRequest> resources,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkResourceResult();
        var resourceList = resources.ToList();
        result.TotalProcessed = resourceList.Count;

        foreach (var request in resourceList)
        {
            try
            {
                var existing = await _context.Set<LocalizationResource>()
                    .FirstOrDefaultAsync(r => r.Key == request.Key && r.CultureName == request.CultureName, cancellationToken);

                if (existing != null)
                {
                    if (request.OverwriteExisting && !existing.IsSystem)
                    {
                        existing.UpdateValue(request.Value);
                        existing.UpdateMetadata(request.Group, request.Description);
                        existing.SetModified(_dateTime.Now, _currentUser.UserId);
                        result.UpdatedCount++;
                    }
                    else
                    {
                        result.SkippedCount++;
                    }
                }
                else
                {
                    var resource = new LocalizationResource(request.Key, request.CultureName, request.Value);
                    resource.UpdateMetadata(request.Group, request.Description);
                    resource.SetCreated(_dateTime.Now, _currentUser.UserId ?? "system");

                    _context.Set<LocalizationResource>().Add(resource);
                    result.CreatedCount++;
                }
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add(new BulkResourceError
                {
                    Key = request.Key,
                    CultureName = request.CultureName,
                    ErrorMessage = ex.Message
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Imported {Total} resources: {Created} created, {Updated} updated, {Skipped} skipped, {Errors} errors",
            result.TotalProcessed, result.CreatedCount, result.UpdatedCount, result.SkippedCount, result.ErrorCount);

        return result;
    }

    public async Task<Result<IEnumerable<ExportResourceResponse>>> ExportResourcesAsync(
        ExportResourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<LocalizationResource>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.CultureName))
        {
            query = query.Where(r => r.CultureName == request.CultureName);
        }

        if (!string.IsNullOrWhiteSpace(request.Group))
        {
            query = query.Where(r => r.Group == request.Group);
        }

        var resources = await query
            .OrderBy(r => r.Key)
            .ThenBy(r => r.CultureName)
            .Select(r => new ExportResourceResponse
            {
                Key = r.Key,
                CultureName = r.CultureName,
                Value = r.Value,
                Group = r.Group,
                Description = r.Description
            })
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<ExportResourceResponse>>.Success(resources);
    }

    #endregion

    #region Language Management

    public async Task<Result<IEnumerable<LanguageResponse>>> GetLanguagesAsync(
        CancellationToken cancellationToken = default)
    {
        var languages = await _context.Set<SupportedLanguage>()
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.DisplayName)
            .ToListAsync(cancellationToken);

        var resourceCounts = await _context.Set<LocalizationResource>()
            .GroupBy(r => r.CultureName)
            .Select(g => new { CultureName = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CultureName, x => x.Count, cancellationToken);

        var result = languages.Select(l => new LanguageResponse
        {
            Id = l.Id,
            CultureName = l.CultureName,
            DisplayName = l.DisplayName,
            NativeName = l.NativeName,
            FlagCode = l.FlagCode,
            IsEnabled = l.IsEnabled,
            IsDefault = l.IsDefault,
            IsRtl = l.IsRtl,
            SortOrder = l.SortOrder,
            ResourceCount = resourceCounts.GetValueOrDefault(l.CultureName, 0)
        });

        return Result<IEnumerable<LanguageResponse>>.Success(result);
    }

    public async Task<Result<Guid>> AddLanguageAsync(
        AddLanguageRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<SupportedLanguage>()
            .FirstOrDefaultAsync(l => l.CultureName == request.CultureName, cancellationToken);

        if (existing != null)
            return Result<Guid>.Conflict("This language is already configured");

        var language = new SupportedLanguage(request.CultureName, request.DisplayName, request.NativeName);
        language.Update(request.DisplayName, request.NativeName, request.FlagCode, request.IsRtl, request.SortOrder);
        language.SetCreated(_dateTime.Now, _currentUser.UserId ?? "system");

        _context.Set<SupportedLanguage>().Add(language);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added language {CultureName}", request.CultureName);

        return language.Id;
    }

    public async Task<Result> UpdateLanguageAsync(
        Guid languageId,
        UpdateLanguageRequest request,
        CancellationToken cancellationToken = default)
    {
        var language = await _context.Set<SupportedLanguage>()
            .FirstOrDefaultAsync(l => l.Id == languageId, cancellationToken);

        if (language == null)
            return Result.NotFound("Language not found");

        language.Update(
            request.DisplayName ?? language.DisplayName,
            request.NativeName ?? language.NativeName,
            request.FlagCode ?? language.FlagCode,
            request.IsRtl ?? language.IsRtl,
            request.SortOrder ?? language.SortOrder);

        language.SetModified(_dateTime.Now, _currentUser.UserId);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated language {LanguageId}", languageId);

        return Result.Success();
    }

    public async Task<Result> SetDefaultLanguageAsync(
        Guid languageId,
        CancellationToken cancellationToken = default)
    {
        var language = await _context.Set<SupportedLanguage>()
            .FirstOrDefaultAsync(l => l.Id == languageId, cancellationToken);

        if (language == null)
            return Result.NotFound("Language not found");

        // Clear current default
        var currentDefaults = await _context.Set<SupportedLanguage>()
            .Where(l => l.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var defaultLang in currentDefaults)
        {
            defaultLang.ClearDefault();
            defaultLang.SetModified(_dateTime.Now, _currentUser.UserId);
        }

        // Set new default
        language.SetAsDefault();
        language.SetModified(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Set language {LanguageId} as default", languageId);

        return Result.Success();
    }

    public async Task<Result> EnableLanguageAsync(
        Guid languageId,
        CancellationToken cancellationToken = default)
    {
        var language = await _context.Set<SupportedLanguage>()
            .FirstOrDefaultAsync(l => l.Id == languageId, cancellationToken);

        if (language == null)
            return Result.NotFound("Language not found");

        language.Enable();
        language.SetModified(_dateTime.Now, _currentUser.UserId);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Enabled language {LanguageId}", languageId);

        return Result.Success();
    }

    public async Task<Result> DisableLanguageAsync(
        Guid languageId,
        CancellationToken cancellationToken = default)
    {
        var language = await _context.Set<SupportedLanguage>()
            .FirstOrDefaultAsync(l => l.Id == languageId, cancellationToken);

        if (language == null)
            return Result.NotFound("Language not found");

        if (language.IsDefault)
            return Result.Forbidden("Cannot disable the default language");

        language.Disable();
        language.SetModified(_dateTime.Now, _currentUser.UserId);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Disabled language {LanguageId}", languageId);

        return Result.Success();
    }

    #endregion

    #region Resource Groups

    public async Task<Result<IEnumerable<string>>> GetResourceGroupsAsync(
        CancellationToken cancellationToken = default)
    {
        var groups = await _context.Set<LocalizationResource>()
            .Where(r => r.Group != null)
            .Select(r => r.Group!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<string>>.Success(groups);
    }

    public async Task<Result<IEnumerable<ResourceResponse>>> GetResourcesByGroupAsync(
        string group,
        string? cultureName = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<LocalizationResource>()
            .Where(r => r.Group == group);

        if (!string.IsNullOrWhiteSpace(cultureName))
        {
            query = query.Where(r => r.CultureName == cultureName);
        }

        var resources = await query
            .OrderBy(r => r.Key)
            .Select(r => new ResourceResponse
            {
                Id = r.Id,
                Key = r.Key,
                CultureName = r.CultureName,
                Value = r.Value,
                Group = r.Group,
                Description = r.Description,
                IsSystem = r.IsSystem,
                CreatedAt = r.CreatedAt,
                LastModifiedAt = r.LastModifiedAt
            })
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<ResourceResponse>>.Success(resources);
    }

    #endregion

    #region Statistics

    public async Task<Result<LocalizationStatisticsResponse>> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var languages = await _context.Set<SupportedLanguage>().ToListAsync(cancellationToken);
        var resources = await _context.Set<LocalizationResource>().ToListAsync(cancellationToken);

        var defaultCulture = languages.FirstOrDefault(l => l.IsDefault)?.CultureName ?? "en-US";
        var defaultResourceCount = resources.Count(r => r.CultureName == defaultCulture);

        var stats = new LocalizationStatisticsResponse
        {
            TotalLanguages = languages.Count,
            EnabledLanguages = languages.Count(l => l.IsEnabled),
            TotalResources = resources.Count,
            TotalGroups = resources.Where(r => r.Group != null).Select(r => r.Group).Distinct().Count(),
            LanguageCounts = languages.Select(l =>
            {
                var count = resources.Count(r => r.CultureName == l.CultureName);
                return new LanguageResourceCount
                {
                    CultureName = l.CultureName,
                    DisplayName = l.DisplayName,
                    ResourceCount = count,
                    CompletionPercentage = defaultResourceCount > 0
                        ? Math.Round(count * 100.0 / defaultResourceCount, 1)
                        : 0
                };
            }).ToList(),
            GroupCounts = resources
                .Where(r => r.Group != null)
                .GroupBy(r => r.Group!)
                .Select(g => new GroupResourceCount
                {
                    Group = g.Key,
                    ResourceCount = g.Count()
                })
                .OrderByDescending(g => g.ResourceCount)
                .ToList()
        };

        return stats;
    }

    public async Task<Result<IEnumerable<MissingResourceResponse>>> GetMissingResourcesAsync(
        string targetCultureName,
        CancellationToken cancellationToken = default)
    {
        var defaultLanguage = await _context.Set<SupportedLanguage>()
            .FirstOrDefaultAsync(l => l.IsDefault, cancellationToken);

        var defaultCulture = defaultLanguage?.CultureName ?? "en-US";

        // Get all keys from default culture
        var defaultKeys = await _context.Set<LocalizationResource>()
            .Where(r => r.CultureName == defaultCulture)
            .Select(r => new { r.Key, r.Group, r.Description, r.Value })
            .ToListAsync(cancellationToken);

        // Get existing keys for target culture
        var targetKeys = await _context.Set<LocalizationResource>()
            .Where(r => r.CultureName == targetCultureName)
            .Select(r => r.Key)
            .ToHashSetAsync(cancellationToken);

        // Find missing keys
        var missing = defaultKeys
            .Where(d => !targetKeys.Contains(d.Key))
            .Select(d => new MissingResourceResponse
            {
                Key = d.Key,
                Group = d.Group ?? "Other",
                Description = d.Description,
                DefaultValue = d.Value
            })
            .OrderBy(m => m.Group)
            .ThenBy(m => m.Key);

        return Result<IEnumerable<MissingResourceResponse>>.Success(missing);
    }

    #endregion
}
