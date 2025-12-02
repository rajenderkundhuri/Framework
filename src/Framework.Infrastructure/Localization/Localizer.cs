using System.Globalization;
using Framework.Application.Localization;

namespace Framework.Infrastructure.Localization;

/// <summary>
/// Generic localizer that provides localized strings for a specific type
/// </summary>
/// <typeparam name="T">The type to localize (used for resource namespacing)</typeparam>
public class Localizer<T> : ILocalizer<T>
{
    private readonly ILocalizationService _localizationService;
    private readonly string _baseName;

    public Localizer(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        _baseName = typeof(T).FullName?.Replace("+", ".") ?? typeof(T).Name;
    }

    public LocalizedString this[string key]
    {
        get
        {
            var fullKey = GetFullKey(key);
            var value = _localizationService.GetString(fullKey);
            var resourceNotFound = value == fullKey;

            return new LocalizedString(key, value, resourceNotFound)
            {
                SearchedLocation = _baseName
            };
        }
    }

    public LocalizedString this[string key, params object[] args]
    {
        get
        {
            var fullKey = GetFullKey(key);
            var value = _localizationService.GetString(fullKey, args);
            var resourceNotFound = value == fullKey;

            return new LocalizedString(key, value, resourceNotFound)
            {
                SearchedLocation = _baseName
            };
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures = false)
    {
        var allStrings = _localizationService.GetAllStrings();
        var prefix = _baseName + ".";

        foreach (var str in allStrings)
        {
            if (str.Key.StartsWith(prefix))
            {
                yield return new LocalizedString(
                    str.Key.Substring(prefix.Length),
                    str.Value,
                    str.ResourceNotFound)
                {
                    SearchedLocation = _baseName
                };
            }
        }

        if (includeParentCultures)
        {
            var currentCulture = _localizationService.CurrentCulture;
            if (currentCulture.Parent != null && !currentCulture.Parent.Equals(CultureInfo.InvariantCulture))
            {
                var parentStrings = _localizationService.GetAllStrings(currentCulture.Parent);
                foreach (var str in parentStrings)
                {
                    if (str.Key.StartsWith(prefix))
                    {
                        yield return new LocalizedString(
                            str.Key.Substring(prefix.Length),
                            str.Value,
                            str.ResourceNotFound)
                        {
                            SearchedLocation = _baseName
                        };
                    }
                }
            }
        }
    }

    private string GetFullKey(string key)
    {
        // If key already starts with base name, return as is
        if (key.StartsWith(_baseName + "."))
        {
            return key;
        }

        // If key contains dots and doesn't start with base name,
        // assume it's a fully qualified key
        if (key.Contains('.') && !key.StartsWith(_baseName))
        {
            return key;
        }

        // Otherwise, prepend base name
        return $"{_baseName}.{key}";
    }
}

/// <summary>
/// Shared resources localizer for common strings
/// </summary>
public class SharedResources { }

/// <summary>
/// Error messages localizer
/// </summary>
public class ErrorResources { }

/// <summary>
/// Validation messages localizer
/// </summary>
public class ValidationResources { }

/// <summary>
/// Common labels localizer
/// </summary>
public class CommonResources { }
