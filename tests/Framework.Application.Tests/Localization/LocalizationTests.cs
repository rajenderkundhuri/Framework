using Framework.Application.Localization;
using Shouldly;

namespace Framework.Application.Tests.Localization;

public class LocalizedStringTests
{
    [Fact]
    public void DefaultConstructor_ShouldHaveDefaults()
    {
        // Act
        var str = new LocalizedString();

        // Assert
        str.Key.ShouldBe(string.Empty);
        str.Value.ShouldBe(string.Empty);
        str.ResourceNotFound.ShouldBeFalse();
        str.SearchedLocation.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithValues_ShouldSetProperties()
    {
        // Act
        var str = new LocalizedString("key", "value", true);

        // Assert
        str.Key.ShouldBe("key");
        str.Value.ShouldBe("value");
        str.ResourceNotFound.ShouldBeTrue();
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnValue()
    {
        // Arrange
        var str = new LocalizedString("key", "Hello World");

        // Act
        string result = str;

        // Assert
        result.ShouldBe("Hello World");
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var str = new LocalizedString("key", "Hello World");

        // Act
        var result = str.ToString();

        // Assert
        result.ShouldBe("Hello World");
    }
}

public class LocalizationSettingsTests
{
    [Fact]
    public void SectionName_ShouldBeLocalization()
    {
        LocalizationSettings.SectionName.ShouldBe("Localization");
    }

    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var settings = new LocalizationSettings();

        // Assert
        settings.DefaultCulture.ShouldBe("en-US");
        settings.SupportedCultures.ShouldContain("en-US");
        settings.ResourcesPath.ShouldBe("Resources");
        settings.ResourceFileType.ShouldBe(ResourceFileType.Json);
        settings.FallbackBehavior.ShouldBe(FallbackBehavior.ParentCulture);
        settings.CacheResources.ShouldBeTrue();
        settings.UseRequestLocalization.ShouldBeTrue();
        settings.CultureHeaderName.ShouldBe("Accept-Language");
        settings.CultureQueryParameterName.ShouldBe("culture");
        settings.CultureCookieName.ShouldBe(".AspNetCore.Culture");
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        // Act
        var settings = new LocalizationSettings
        {
            DefaultCulture = "de-DE",
            SupportedCultures = new List<string> { "de-DE", "en-US" },
            ResourcesPath = "Lang",
            ResourceFileType = ResourceFileType.Resx,
            FallbackBehavior = FallbackBehavior.ReturnKey,
            CacheResources = false
        };

        // Assert
        settings.DefaultCulture.ShouldBe("de-DE");
        settings.SupportedCultures.Count.ShouldBe(2);
        settings.ResourcesPath.ShouldBe("Lang");
        settings.ResourceFileType.ShouldBe(ResourceFileType.Resx);
        settings.FallbackBehavior.ShouldBe(FallbackBehavior.ReturnKey);
        settings.CacheResources.ShouldBeFalse();
    }
}

public class ResourceFileTypeTests
{
    [Fact]
    public void ResourceFileType_ShouldHaveExpectedValues()
    {
        ((int)ResourceFileType.Json).ShouldBe(0);
        ((int)ResourceFileType.Resx).ShouldBe(1);
        ((int)ResourceFileType.Yaml).ShouldBe(2);
    }
}

public class FallbackBehaviorTests
{
    [Fact]
    public void FallbackBehavior_ShouldHaveExpectedValues()
    {
        ((int)FallbackBehavior.ParentCulture).ShouldBe(0);
        ((int)FallbackBehavior.DefaultCulture).ShouldBe(1);
        ((int)FallbackBehavior.ReturnKey).ShouldBe(2);
        ((int)FallbackBehavior.ReturnEmpty).ShouldBe(3);
    }
}
