using Framework.Application.Common.Models;

namespace Framework.Application.Localization;

/// <summary>
/// Service for managing localization resources
/// </summary>
public interface IResourceManagementService
{
    // Resource CRUD
    Task<Result<PagedList<ResourceResponse>>> GetResourcesAsync(ResourceListRequest request, CancellationToken cancellationToken = default);
    Task<Result<ResourceResponse>> GetResourceByIdAsync(Guid resourceId, CancellationToken cancellationToken = default);
    Task<Result<ResourceResponse>> GetResourceAsync(string key, string cultureName, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateResourceAsync(CreateResourceRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateResourceAsync(Guid resourceId, UpdateResourceRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteResourceAsync(Guid resourceId, CancellationToken cancellationToken = default);

    // Bulk Operations
    Task<Result<BulkResourceResult>> ImportResourcesAsync(IEnumerable<ImportResourceRequest> resources, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ExportResourceResponse>>> ExportResourcesAsync(ExportResourceRequest request, CancellationToken cancellationToken = default);

    // Language Management
    Task<Result<IEnumerable<LanguageResponse>>> GetLanguagesAsync(CancellationToken cancellationToken = default);
    Task<Result<Guid>> AddLanguageAsync(AddLanguageRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateLanguageAsync(Guid languageId, UpdateLanguageRequest request, CancellationToken cancellationToken = default);
    Task<Result> SetDefaultLanguageAsync(Guid languageId, CancellationToken cancellationToken = default);
    Task<Result> EnableLanguageAsync(Guid languageId, CancellationToken cancellationToken = default);
    Task<Result> DisableLanguageAsync(Guid languageId, CancellationToken cancellationToken = default);

    // Resource Groups
    Task<Result<IEnumerable<string>>> GetResourceGroupsAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ResourceResponse>>> GetResourcesByGroupAsync(string group, string? cultureName = null, CancellationToken cancellationToken = default);

    // Statistics
    Task<Result<LocalizationStatisticsResponse>> GetStatisticsAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<MissingResourceResponse>>> GetMissingResourcesAsync(string targetCultureName, CancellationToken cancellationToken = default);
}

#region Request DTOs

/// <summary>
/// Resource list request
/// </summary>
public class ResourceListRequest
{
    public string? SearchTerm { get; set; }
    public string? CultureName { get; set; }
    public string? Group { get; set; }
    public bool? IsSystem { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = "Key";
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// Create resource request
/// </summary>
public class CreateResourceRequest
{
    public string Key { get; set; } = string.Empty;
    public string CultureName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Group { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Update resource request
/// </summary>
public class UpdateResourceRequest
{
    public string? Value { get; set; }
    public string? Group { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Import resource request
/// </summary>
public class ImportResourceRequest
{
    public string Key { get; set; } = string.Empty;
    public string CultureName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Group { get; set; }
    public string? Description { get; set; }
    public bool OverwriteExisting { get; set; } = false;
}

/// <summary>
/// Export resource request
/// </summary>
public class ExportResourceRequest
{
    public string? CultureName { get; set; }
    public string? Group { get; set; }
    public ExportFormat Format { get; set; } = ExportFormat.Json;
}

/// <summary>
/// Add language request
/// </summary>
public class AddLanguageRequest
{
    public string CultureName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public string? FlagCode { get; set; }
    public bool IsRtl { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Update language request
/// </summary>
public class UpdateLanguageRequest
{
    public string? DisplayName { get; set; }
    public string? NativeName { get; set; }
    public string? FlagCode { get; set; }
    public bool? IsRtl { get; set; }
    public int? SortOrder { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// Resource response
/// </summary>
public class ResourceResponse
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string CultureName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Group { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
}

/// <summary>
/// Language response
/// </summary>
public class LanguageResponse
{
    public Guid Id { get; set; }
    public string CultureName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public string? FlagCode { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsDefault { get; set; }
    public bool IsRtl { get; set; }
    public int SortOrder { get; set; }
    public int ResourceCount { get; set; }
}

/// <summary>
/// Bulk resource operation result
/// </summary>
public class BulkResourceResult
{
    public int TotalProcessed { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<BulkResourceError> Errors { get; set; } = new();
}

/// <summary>
/// Bulk resource error
/// </summary>
public class BulkResourceError
{
    public string Key { get; set; } = string.Empty;
    public string CultureName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Export resource response
/// </summary>
public class ExportResourceResponse
{
    public string Key { get; set; } = string.Empty;
    public string CultureName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Group { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Localization statistics
/// </summary>
public class LocalizationStatisticsResponse
{
    public int TotalLanguages { get; set; }
    public int EnabledLanguages { get; set; }
    public int TotalResources { get; set; }
    public int TotalGroups { get; set; }
    public List<LanguageResourceCount> LanguageCounts { get; set; } = new();
    public List<GroupResourceCount> GroupCounts { get; set; } = new();
}

/// <summary>
/// Language resource count
/// </summary>
public class LanguageResourceCount
{
    public string CultureName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int ResourceCount { get; set; }
    public double CompletionPercentage { get; set; }
}

/// <summary>
/// Group resource count
/// </summary>
public class GroupResourceCount
{
    public string Group { get; set; } = string.Empty;
    public int ResourceCount { get; set; }
}

/// <summary>
/// Missing resource response
/// </summary>
public class MissingResourceResponse
{
    public string Key { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Export formats
/// </summary>
public enum ExportFormat
{
    Json = 0,
    Csv = 1,
    Resx = 2
}

#endregion
