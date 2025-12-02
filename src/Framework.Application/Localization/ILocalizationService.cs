using System.Globalization;

namespace Framework.Application.Localization;

/// <summary>
/// Interface for localization services
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets a localized string
    /// </summary>
    string GetString(string key);

    /// <summary>
    /// Gets a localized string with format arguments
    /// </summary>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Gets a localized string for specific culture
    /// </summary>
    string GetString(string key, CultureInfo culture);

    /// <summary>
    /// Gets all strings for current culture
    /// </summary>
    IEnumerable<LocalizedString> GetAllStrings();

    /// <summary>
    /// Gets all strings for specific culture
    /// </summary>
    IEnumerable<LocalizedString> GetAllStrings(CultureInfo culture);

    /// <summary>
    /// Current culture
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Supported cultures
    /// </summary>
    IEnumerable<CultureInfo> SupportedCultures { get; }
}

/// <summary>
/// Represents a localized string
/// </summary>
public class LocalizedString
{
    /// <summary>
    /// Resource key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Localized value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Whether the resource was not found
    /// </summary>
    public bool ResourceNotFound { get; set; }

    /// <summary>
    /// Search location
    /// </summary>
    public string? SearchedLocation { get; set; }

    public LocalizedString() { }

    public LocalizedString(string key, string value, bool resourceNotFound = false)
    {
        Key = key;
        Value = value;
        ResourceNotFound = resourceNotFound;
    }

    public static implicit operator string(LocalizedString localizedString)
        => localizedString.Value;

    public override string ToString() => Value;
}

/// <summary>
/// Settings for localization
/// </summary>
public class LocalizationSettings
{
    public const string SectionName = "Localization";

    /// <summary>
    /// Default culture
    /// </summary>
    public string DefaultCulture { get; set; } = "en-US";

    /// <summary>
    /// Supported cultures
    /// </summary>
    public List<string> SupportedCultures { get; set; } = new() { "en-US" };

    /// <summary>
    /// Path to resource files
    /// </summary>
    public string ResourcesPath { get; set; } = "Resources";

    /// <summary>
    /// Resource file type
    /// </summary>
    public ResourceFileType ResourceFileType { get; set; } = ResourceFileType.Json;

    /// <summary>
    /// Fallback behavior
    /// </summary>
    public FallbackBehavior FallbackBehavior { get; set; } = FallbackBehavior.ParentCulture;

    /// <summary>
    /// Whether to cache resources
    /// </summary>
    public bool CacheResources { get; set; } = true;

    /// <summary>
    /// Whether to use culture from request header
    /// </summary>
    public bool UseRequestLocalization { get; set; } = true;

    /// <summary>
    /// Request header name for culture
    /// </summary>
    public string CultureHeaderName { get; set; } = "Accept-Language";

    /// <summary>
    /// Query parameter name for culture
    /// </summary>
    public string CultureQueryParameterName { get; set; } = "culture";

    /// <summary>
    /// Cookie name for culture
    /// </summary>
    public string CultureCookieName { get; set; } = ".AspNetCore.Culture";
}

/// <summary>
/// Resource file types
/// </summary>
public enum ResourceFileType
{
    Json = 0,
    Resx = 1,
    Yaml = 2
}

/// <summary>
/// Fallback behavior when resource not found
/// </summary>
public enum FallbackBehavior
{
    /// <summary>
    /// Fall back to parent culture
    /// </summary>
    ParentCulture = 0,

    /// <summary>
    /// Fall back to default culture
    /// </summary>
    DefaultCulture = 1,

    /// <summary>
    /// Return key as value
    /// </summary>
    ReturnKey = 2,

    /// <summary>
    /// Return empty string
    /// </summary>
    ReturnEmpty = 3
}

/// <summary>
/// Interface for localized resources by type
/// </summary>
/// <typeparam name="T">Type to localize</typeparam>
public interface ILocalizer<T>
{
    /// <summary>
    /// Gets a localized string
    /// </summary>
    LocalizedString this[string key] { get; }

    /// <summary>
    /// Gets a localized string with format arguments
    /// </summary>
    LocalizedString this[string key, params object[] args] { get; }

    /// <summary>
    /// Gets all strings
    /// </summary>
    IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures = false);
}
