using System.Collections.Concurrent;
using System.Globalization;
using Framework.Application.Localization;
using Framework.Domain.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Localization;

/// <summary>
/// Database-backed localization service with file fallback
/// </summary>
public class DatabaseLocalizationService : ILocalizationService
{
    private readonly DbContext _context;
    private readonly LocalizationSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DatabaseLocalizationService> _logger;
    private readonly JsonLocalizationService _fileLocalizationService;
    private CultureInfo _currentCulture;

    private const string CacheKeyPrefix = "Localization_";
    private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(30);

    public DatabaseLocalizationService(
        DbContext context,
        IOptions<LocalizationSettings> settings,
        IMemoryCache cache,
        ILogger<DatabaseLocalizationService> logger,
        JsonLocalizationService fileLocalizationService)
    {
        _context = context;
        _settings = settings.Value;
        _cache = cache;
        _logger = logger;
        _fileLocalizationService = fileLocalizationService;
        _currentCulture = CultureInfo.GetCultureInfo(_settings.DefaultCulture);
    }

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            _currentCulture = value;
            _fileLocalizationService.CurrentCulture = value;
        }
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
        try
        {
            return string.Format(value, args);
        }
        catch (FormatException)
        {
            _logger.LogWarning("Failed to format localized string {Key} with provided arguments", key);
            return value;
        }
    }

    public string GetString(string key, CultureInfo culture)
    {
        var cacheKey = $"{CacheKeyPrefix}{culture.Name}_{key}";

        if (_settings.CacheResources && _cache.TryGetValue(cacheKey, out string? cachedValue) && cachedValue != null)
        {
            return cachedValue;
        }

        // Try database first
        var dbValue = GetFromDatabase(key, culture);
        if (dbValue != null)
        {
            if (_settings.CacheResources)
            {
                _cache.Set(cacheKey, dbValue, DefaultCacheExpiration);
            }
            return dbValue;
        }

        // Fall back to file-based localization
        var fileValue = _fileLocalizationService.GetString(key, culture);

        // Only cache if it's not the key itself (meaning resource was found)
        if (_settings.CacheResources && fileValue != key)
        {
            _cache.Set(cacheKey, fileValue, DefaultCacheExpiration);
        }

        return fileValue;
    }

    public IEnumerable<LocalizedString> GetAllStrings()
    {
        return GetAllStrings(CurrentCulture);
    }

    public IEnumerable<LocalizedString> GetAllStrings(CultureInfo culture)
    {
        var result = new Dictionary<string, LocalizedString>();

        // Get from file first (base)
        foreach (var str in _fileLocalizationService.GetAllStrings(culture))
        {
            result[str.Key] = str;
        }

        // Override/add from database
        try
        {
            var dbResources = _context.Set<LocalizationResource>()
                .Where(r => r.CultureName == culture.Name)
                .AsNoTracking()
                .ToList();

            foreach (var resource in dbResources)
            {
                result[resource.Key] = new LocalizedString(resource.Key, resource.Value)
                {
                    SearchedLocation = "Database"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load resources from database for culture {Culture}", culture.Name);
        }

        return result.Values;
    }

    private string? GetFromDatabase(string key, CultureInfo culture)
    {
        try
        {
            // Try exact culture match
            var resource = _context.Set<LocalizationResource>()
                .AsNoTracking()
                .FirstOrDefault(r => r.Key == key && r.CultureName == culture.Name);

            if (resource != null)
            {
                return resource.Value;
            }

            // Try neutral culture (e.g., "en" for "en-US")
            if (culture.Parent != null && !culture.Parent.Equals(CultureInfo.InvariantCulture))
            {
                resource = _context.Set<LocalizationResource>()
                    .AsNoTracking()
                    .FirstOrDefault(r => r.Key == key && r.CultureName == culture.Parent.Name);

                if (resource != null)
                {
                    return resource.Value;
                }
            }

            // Try default culture fallback
            if (_settings.FallbackBehavior == FallbackBehavior.DefaultCulture &&
                !culture.Name.Equals(_settings.DefaultCulture, StringComparison.OrdinalIgnoreCase))
            {
                resource = _context.Set<LocalizationResource>()
                    .AsNoTracking()
                    .FirstOrDefault(r => r.Key == key && r.CultureName == _settings.DefaultCulture);

                if (resource != null)
                {
                    return resource.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get resource {Key} for culture {Culture} from database", key, culture.Name);
        }

        return null;
    }

    /// <summary>
    /// Clears the localization cache
    /// </summary>
    public void ClearCache()
    {
        // Clear file cache
        _fileLocalizationService.ClearCache();

        // Note: IMemoryCache doesn't have a built-in clear method
        // In production, you might use IDistributedCache with explicit key management
        // or implement a custom cache wrapper with clear functionality
        _logger.LogInformation("Localization cache cleared");
    }

    /// <summary>
    /// Invalidates cache for specific key
    /// </summary>
    public void InvalidateKey(string key)
    {
        foreach (var culture in SupportedCultures)
        {
            var cacheKey = $"{CacheKeyPrefix}{culture.Name}_{key}";
            _cache.Remove(cacheKey);
        }
    }

    /// <summary>
    /// Invalidates cache for specific culture
    /// </summary>
    public void InvalidateCulture(string cultureName)
    {
        // Since we can't enumerate IMemoryCache keys, this requires
        // either using IDistributedCache or maintaining a key registry
        _logger.LogInformation("Cache invalidation requested for culture {Culture}", cultureName);
    }
}
