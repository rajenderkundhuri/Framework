using Framework.Application.FeatureFlags;
using Shouldly;

namespace Framework.Application.Tests.FeatureFlags;

public class FeatureDefinitionTests
{
    [Fact]
    public void NewDefinition_ShouldHaveDefaults()
    {
        // Act
        var definition = new FeatureDefinition();

        // Assert
        definition.Name.ShouldBe(string.Empty);
        definition.Enabled.ShouldBeFalse();
        definition.Description.ShouldBeNull();
        definition.Filters.ShouldNotBeNull();
        definition.Filters.ShouldBeEmpty();
        definition.Parameters.ShouldNotBeNull();
        definition.Parameters.ShouldBeEmpty();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        // Act
        var definition = new FeatureDefinition
        {
            Name = "NewFeature",
            Enabled = true,
            Description = "A new experimental feature",
            Filters = new List<FeatureFilter>
            {
                new FeatureFilter { Name = "Percentage", Parameters = { ["Value"] = 50 } }
            },
            Parameters = new Dictionary<string, object> { ["key"] = "value" }
        };

        // Assert
        definition.Name.ShouldBe("NewFeature");
        definition.Enabled.ShouldBeTrue();
        definition.Description.ShouldBe("A new experimental feature");
        definition.Filters.Count.ShouldBe(1);
        definition.Parameters["key"].ShouldBe("value");
    }
}

public class FeatureFilterTests
{
    [Fact]
    public void NewFilter_ShouldHaveDefaults()
    {
        // Act
        var filter = new FeatureFilter();

        // Assert
        filter.Name.ShouldBe(string.Empty);
        filter.Parameters.ShouldNotBeNull();
        filter.Parameters.ShouldBeEmpty();
    }

    [Fact]
    public void Filter_ShouldAllowParameters()
    {
        // Act
        var filter = new FeatureFilter
        {
            Name = "Targeting",
            Parameters = new Dictionary<string, object>
            {
                ["Users"] = new[] { "user1", "user2" },
                ["Groups"] = new[] { "beta-testers" }
            }
        };

        // Assert
        filter.Name.ShouldBe("Targeting");
        filter.Parameters.Count.ShouldBe(2);
    }
}

public class FeatureFlagSettingsTests
{
    [Fact]
    public void SectionName_ShouldBeFeatureFlags()
    {
        FeatureFlagSettings.SectionName.ShouldBe("FeatureFlags");
    }

    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var settings = new FeatureFlagSettings();

        // Assert
        settings.Enabled.ShouldBeTrue();
        settings.CacheDurationSeconds.ShouldBe(30);
        settings.Provider.ShouldBe(FeatureFlagProvider.Configuration);
        settings.Features.ShouldNotBeNull();
        settings.Features.ShouldBeEmpty();
    }
}

public class FeatureFlagProviderTests
{
    [Fact]
    public void Provider_ShouldHaveExpectedValues()
    {
        ((int)FeatureFlagProvider.Configuration).ShouldBe(0);
        ((int)FeatureFlagProvider.Database).ShouldBe(1);
        ((int)FeatureFlagProvider.External).ShouldBe(2);
    }
}

public class FeatureGateAttributeTests
{
    [Fact]
    public void Constructor_WithSingleFeature_ShouldSetFeatures()
    {
        // Act
        var attr = new FeatureGateAttribute("FeatureA");

        // Assert
        attr.Features.Length.ShouldBe(1);
        attr.Features[0].ShouldBe("FeatureA");
        attr.Requirement.ShouldBe(FeatureGateRequirement.All);
    }

    [Fact]
    public void Constructor_WithMultipleFeatures_ShouldSetFeatures()
    {
        // Act
        var attr = new FeatureGateAttribute("FeatureA", "FeatureB", "FeatureC");

        // Assert
        attr.Features.Length.ShouldBe(3);
    }

    [Fact]
    public void Requirement_ShouldBeSettable()
    {
        // Act
        var attr = new FeatureGateAttribute("F1", "F2") { Requirement = FeatureGateRequirement.Any };

        // Assert
        attr.Requirement.ShouldBe(FeatureGateRequirement.Any);
    }
}

public class FeatureGateRequirementTests
{
    [Fact]
    public void Requirement_ShouldHaveExpectedValues()
    {
        ((int)FeatureGateRequirement.All).ShouldBe(0);
        ((int)FeatureGateRequirement.Any).ShouldBe(1);
    }
}
