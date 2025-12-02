using System.Globalization;
using Framework.Application.Localization;
using Framework.Infrastructure.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Framework.Infrastructure.Tests.Localization;

public class JsonLocalizationServiceTests : IDisposable
{
    private readonly ILogger<JsonLocalizationService> _logger;
    private readonly string _tempPath;

    public JsonLocalizationServiceTests()
    {
        _logger = Substitute.For<ILogger<JsonLocalizationService>>();
        _tempPath = Path.Combine(Path.GetTempPath(), $"localization-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, true);
        }
    }

    private IOptions<LocalizationSettings> CreateOptions(string? resourcesPath = null)
    {
        return Options.Create(new LocalizationSettings
        {
            ResourcesPath = resourcesPath ?? _tempPath,
            DefaultCulture = "en-US",
            SupportedCultures = new List<string> { "en-US", "de-DE", "fr-FR" },
            FallbackBehavior = FallbackBehavior.DefaultCulture
        });
    }

    private void CreateResourceFile(string culture, Dictionary<string, string> resources)
    {
        var filePath = Path.Combine(_tempPath, $"{culture}.json");
        var json = System.Text.Json.JsonSerializer.Serialize(resources);
        File.WriteAllText(filePath, json);
    }

    [Fact]
    public void GetString_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        CreateResourceFile("en-US", new Dictionary<string, string> { ["greeting"] = "Hello" });
        var service = new JsonLocalizationService(CreateOptions(), _logger);

        // Act
        var result = service.GetString("greeting", CultureInfo.GetCultureInfo("en-US"));

        // Assert
        result.ShouldBe("Hello");
    }

    [Fact]
    public void GetString_WithMissingKey_ShouldReturnKey()
    {
        // Arrange
        CreateResourceFile("en-US", new Dictionary<string, string>());
        var service = new JsonLocalizationService(CreateOptions(), _logger);

        // Act
        var result = service.GetString("missing-key", CultureInfo.GetCultureInfo("en-US"));

        // Assert
        result.ShouldBe("missing-key");
    }

    [Fact]
    public void GetString_WithFallbackToDefaultCulture_ShouldReturnDefault()
    {
        // Arrange
        CreateResourceFile("en-US", new Dictionary<string, string> { ["greeting"] = "Hello" });
        var service = new JsonLocalizationService(CreateOptions(), _logger);

        // Act
        var result = service.GetString("greeting", CultureInfo.GetCultureInfo("de-DE"));

        // Assert
        result.ShouldBe("Hello");
    }

    [Fact]
    public void GetString_WithFormatArguments_ShouldFormat()
    {
        // Arrange
        CreateResourceFile("en-US", new Dictionary<string, string> { ["welcome"] = "Hello, {0}!" });
        var service = new JsonLocalizationService(CreateOptions(), _logger);
        service.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

        // Act
        var result = service.GetString("welcome", "World");

        // Assert
        result.ShouldBe("Hello, World!");
    }

    [Fact]
    public void GetString_WithMultipleFormatArguments_ShouldFormat()
    {
        // Arrange
        CreateResourceFile("en-US", new Dictionary<string, string> { ["message"] = "{0} has {1} items" });
        var service = new JsonLocalizationService(CreateOptions(), _logger);
        service.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

        // Act
        var result = service.GetString("message", "Cart", 5);

        // Assert
        result.ShouldBe("Cart has 5 items");
    }

    [Fact]
    public void GetAllStrings_ShouldReturnAllResources()
    {
        // Arrange
        CreateResourceFile("en-US", new Dictionary<string, string>
        {
            ["greeting"] = "Hello",
            ["farewell"] = "Goodbye"
        });
        var service = new JsonLocalizationService(CreateOptions(), _logger);

        // Act
        var results = service.GetAllStrings(CultureInfo.GetCultureInfo("en-US")).ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldContain(s => s.Key == "greeting" && s.Value == "Hello");
        results.ShouldContain(s => s.Key == "farewell" && s.Value == "Goodbye");
    }

    [Fact]
    public void GetAllStrings_WithMissingCulture_ShouldReturnEmpty()
    {
        // Arrange
        var service = new JsonLocalizationService(CreateOptions(), _logger);

        // Act
        var results = service.GetAllStrings(CultureInfo.GetCultureInfo("xx-XX")).ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void SupportedCultures_ShouldReturnConfiguredCultures()
    {
        // Arrange
        var service = new JsonLocalizationService(CreateOptions(), _logger);

        // Act
        var cultures = service.SupportedCultures.Select(c => c.Name).ToList();

        // Assert
        cultures.ShouldContain("en-US");
        cultures.ShouldContain("de-DE");
        cultures.ShouldContain("fr-FR");
    }

    [Fact]
    public void GetString_WithCaching_ShouldCacheResources()
    {
        // Arrange
        CreateResourceFile("en-US", new Dictionary<string, string> { ["greeting"] = "Hello" });
        var options = Options.Create(new LocalizationSettings
        {
            ResourcesPath = _tempPath,
            CacheResources = true,
            DefaultCulture = "en-US"
        });
        var service = new JsonLocalizationService(options, _logger);

        // Act - call twice
        var result1 = service.GetString("greeting", CultureInfo.GetCultureInfo("en-US"));
        var result2 = service.GetString("greeting", CultureInfo.GetCultureInfo("en-US"));

        // Assert - both should return same value
        result1.ShouldBe("Hello");
        result2.ShouldBe("Hello");
    }

    [Fact]
    public void GetString_WithReturnKeyFallback_ShouldReturnKey()
    {
        // Arrange
        var options = Options.Create(new LocalizationSettings
        {
            ResourcesPath = _tempPath,
            FallbackBehavior = FallbackBehavior.ReturnKey,
            DefaultCulture = "en-US"
        });
        var service = new JsonLocalizationService(options, _logger);

        // Act
        var result = service.GetString("nonexistent", CultureInfo.GetCultureInfo("en-US"));

        // Assert
        result.ShouldBe("nonexistent");
    }

    [Fact]
    public void GetString_WithReturnEmptyFallback_ShouldReturnEmpty()
    {
        // Arrange
        var options = Options.Create(new LocalizationSettings
        {
            ResourcesPath = _tempPath,
            FallbackBehavior = FallbackBehavior.ReturnEmpty,
            DefaultCulture = "en-US"
        });
        var service = new JsonLocalizationService(options, _logger);

        // Act
        var result = service.GetString("nonexistent", CultureInfo.GetCultureInfo("en-US"));

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void GetString_WithSpecificCulture_ShouldUseCorrectFile()
    {
        // Arrange
        CreateResourceFile("en-US", new Dictionary<string, string> { ["greeting"] = "Hello" });
        CreateResourceFile("de-DE", new Dictionary<string, string> { ["greeting"] = "Hallo" });
        var service = new JsonLocalizationService(CreateOptions(), _logger);

        // Act
        var englishResult = service.GetString("greeting", CultureInfo.GetCultureInfo("en-US"));
        var germanResult = service.GetString("greeting", CultureInfo.GetCultureInfo("de-DE"));

        // Assert
        englishResult.ShouldBe("Hello");
        germanResult.ShouldBe("Hallo");
    }

    [Fact]
    public void GetString_WithCurrentCulture_ShouldUseDefault()
    {
        // Arrange
        CreateResourceFile("en-US", new Dictionary<string, string> { ["greeting"] = "Hello" });
        var service = new JsonLocalizationService(CreateOptions(), _logger);

        // Act
        var result = service.GetString("greeting");

        // Assert
        result.ShouldBe("Hello");
    }

    [Fact]
    public void CurrentCulture_ShouldBeSettable()
    {
        // Arrange
        var service = new JsonLocalizationService(CreateOptions(), _logger);

        // Act
        service.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

        // Assert
        service.CurrentCulture.Name.ShouldBe("de-DE");
    }

    [Fact]
    public void ClearCache_ShouldClearResourceCache()
    {
        // Arrange
        CreateResourceFile("en-US", new Dictionary<string, string> { ["greeting"] = "Hello" });
        var options = Options.Create(new LocalizationSettings
        {
            ResourcesPath = _tempPath,
            CacheResources = true,
            DefaultCulture = "en-US"
        });
        var service = new JsonLocalizationService(options, _logger);

        // Load into cache
        service.GetString("greeting", CultureInfo.GetCultureInfo("en-US"));

        // Act
        service.ClearCache();

        // Update file
        CreateResourceFile("en-US", new Dictionary<string, string> { ["greeting"] = "Updated Hello" });

        var result = service.GetString("greeting", CultureInfo.GetCultureInfo("en-US"));

        // Assert
        result.ShouldBe("Updated Hello");
    }
}
