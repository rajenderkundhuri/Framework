using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Framework.Application.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Localization;

/// <summary>
/// JSON file-based localization service
/// </summary>
public class JsonLocalizationService : ILocalizationService
{
    private readonly LocalizationSettings _settings;
    private readonly ILogger<JsonLocalizationService> _logger;
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _resourceCache = new();
    private CultureInfo _currentCulture;

    public JsonLocalizationService(
        IOptions<LocalizationSettings> settings,
        ILogger<JsonLocalizationService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _currentCulture = CultureInfo.GetCultureInfo(_settings.DefaultCulture);
    }

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set => _currentCulture = value;
    }

    public IEnumerable<CultureInfo> SupportedCultures =>
        _settings.SupportedCultures.Select(CultureInfo.GetCultureInfo);

    public string GetString(string key)
    {
        return GetString(key, CurrentCulture);
    }

    public string GetString(string key, params object[] args)
    {
        var value = GetString(key);
        return string.Format(value, args);
    }

    public string GetString(string key, CultureInfo culture)
    {
        var resources = LoadResources(culture);

        if (resources.TryGetValue(key, out var value))
        {
            return value;
        }

        // Try fallback
        value = TryFallback(key, culture);
        if (value != null)
        {
            return value;
        }

        // Return key or empty based on settings
        return _settings.FallbackBehavior switch
        {
            FallbackBehavior.ReturnKey => key,
            FallbackBehavior.ReturnEmpty => string.Empty,
            _ => key
        };
    }

    public IEnumerable<LocalizedString> GetAllStrings()
    {
        return GetAllStrings(CurrentCulture);
    }

    public IEnumerable<LocalizedString> GetAllStrings(CultureInfo culture)
    {
        var resources = LoadResources(culture);

        return resources.Select(kvp => new LocalizedString(kvp.Key, kvp.Value));
    }

    private Dictionary<string, string> LoadResources(CultureInfo culture)
    {
        var cacheKey = culture.Name;

        if (_settings.CacheResources && _resourceCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var resources = new Dictionary<string, string>();

        // Try specific culture file (e.g., en-US.json)
        var specificFile = Path.Combine(_settings.ResourcesPath, $"{culture.Name}.json");
        if (File.Exists(specificFile))
        {
            MergeResources(resources, LoadJsonFile(specificFile));
        }
        else
        {
            // Try neutral culture file (e.g., en.json)
            var neutralFile = Path.Combine(_settings.ResourcesPath, $"{culture.TwoLetterISOLanguageName}.json");
            if (File.Exists(neutralFile))
            {
                MergeResources(resources, LoadJsonFile(neutralFile));
            }
        }

        if (_settings.CacheResources)
        {
            _resourceCache[cacheKey] = resources;
        }

        return resources;
    }

    private Dictionary<string, string> LoadJsonFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (dict == null)
            {
                return new Dictionary<string, string>();
            }

            return FlattenDictionary(dict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load localization file: {FilePath}", filePath);
            return new Dictionary<string, string>();
        }
    }

    private Dictionary<string, string> FlattenDictionary(Dictionary<string, JsonElement> dict, string prefix = "")
    {
        var result = new Dictionary<string, string>();

        foreach (var kvp in dict)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";

            if (kvp.Value.ValueKind == JsonValueKind.Object)
            {
                var nested = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(kvp.Value.GetRawText());
                if (nested != null)
                {
                    foreach (var nestedKvp in FlattenDictionary(nested, key))
                    {
                        result[nestedKvp.Key] = nestedKvp.Value;
                    }
                }
            }
            else
            {
                result[key] = kvp.Value.GetString() ?? string.Empty;
            }
        }

        return result;
    }

    private void MergeResources(Dictionary<string, string> target, Dictionary<string, string> source)
    {
        foreach (var kvp in source)
        {
            target[kvp.Key] = kvp.Value;
        }
    }

    private string? TryFallback(string key, CultureInfo culture)
    {
        switch (_settings.FallbackBehavior)
        {
            case FallbackBehavior.ParentCulture:
                if (culture.Parent != null && !culture.Parent.Equals(CultureInfo.InvariantCulture))
                {
                    var parentResources = LoadResources(culture.Parent);
                    if (parentResources.TryGetValue(key, out var parentValue))
                    {
                        return parentValue;
                    }
                }
                goto case FallbackBehavior.DefaultCulture;

            case FallbackBehavior.DefaultCulture:
                if (!culture.Name.Equals(_settings.DefaultCulture, StringComparison.OrdinalIgnoreCase))
                {
                    var defaultCulture = CultureInfo.GetCultureInfo(_settings.DefaultCulture);
                    var defaultResources = LoadResources(defaultCulture);
                    if (defaultResources.TryGetValue(key, out var defaultValue))
                    {
                        return defaultValue;
                    }
                }
                break;
        }

        return null;
    }

    /// <summary>
    /// Clears the resource cache
    /// </summary>
    public void ClearCache()
    {
        _resourceCache.Clear();
    }
}
