using Framework.Application.FeatureFlags;
using Framework.Infrastructure.FeatureFlags;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Framework.Infrastructure.Tests.FeatureFlags;

public class ConfigurationFeatureManagerTests
{
    private readonly ILogger<ConfigurationFeatureManager> _logger;
    private readonly IEnumerable<IFeatureFilterEvaluator> _defaultEvaluators;

    public ConfigurationFeatureManagerTests()
    {
        _logger = Substitute.For<ILogger<ConfigurationFeatureManager>>();
        _defaultEvaluators = new IFeatureFilterEvaluator[]
        {
            new PercentageFilterEvaluator(),
            new TimeWindowFilterEvaluator(),
            new TargetingFilterEvaluator()
        };
    }

    private ConfigurationFeatureManager CreateManager(Dictionary<string, FeatureDefinition> features, bool enabled = true)
    {
        var options = Options.Create(new FeatureFlagSettings
        {
            Enabled = enabled,
            Features = features
        });
        return new ConfigurationFeatureManager(options, _defaultEvaluators, _logger);
    }

    [Fact]
    public async Task IsEnabledAsync_WhenFeatureEnabled_ShouldReturnTrue()
    {
        // Arrange
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["NewFeature"] = new FeatureDefinition { Name = "NewFeature", Enabled = true }
        };
        var manager = CreateManager(features);

        // Act
        var result = await manager.IsEnabledAsync("NewFeature");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsEnabledAsync_WhenFeatureDisabled_ShouldReturnFalse()
    {
        // Arrange
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["DisabledFeature"] = new FeatureDefinition { Name = "DisabledFeature", Enabled = false }
        };
        var manager = CreateManager(features);

        // Act
        var result = await manager.IsEnabledAsync("DisabledFeature");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsEnabledAsync_WhenFeatureNotFound_ShouldReturnFalse()
    {
        // Arrange
        var manager = CreateManager(new Dictionary<string, FeatureDefinition>());

        // Act
        var result = await manager.IsEnabledAsync("NonExistent");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsEnabledAsync_WhenGloballyDisabled_ShouldReturnFalse()
    {
        // Arrange
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["Feature"] = new FeatureDefinition { Name = "Feature", Enabled = true }
        };
        var manager = CreateManager(features, enabled: false);

        // Act
        var result = await manager.IsEnabledAsync("Feature");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetFeatureDefinitionAsync_ShouldReturnDefinition()
    {
        // Arrange
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["TestFeature"] = new FeatureDefinition
            {
                Name = "TestFeature",
                Enabled = true,
                Description = "A test feature"
            }
        };
        var manager = CreateManager(features);

        // Act
        var result = await manager.GetFeatureDefinitionAsync("TestFeature");

        // Assert
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("TestFeature");
        result.Description.ShouldBe("A test feature");
    }

    [Fact]
    public async Task GetFeatureDefinitionAsync_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var manager = CreateManager(new Dictionary<string, FeatureDefinition>());

        // Act
        var result = await manager.GetFeatureDefinitionAsync("NonExistent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetFeatureNamesAsync_ShouldReturnAllNames()
    {
        // Arrange
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["Feature1"] = new FeatureDefinition { Name = "Feature1", Enabled = true },
            ["Feature2"] = new FeatureDefinition { Name = "Feature2", Enabled = false }
        };
        var manager = CreateManager(features);

        // Act
        var names = new List<string>();
        await foreach (var name in manager.GetFeatureNamesAsync())
        {
            names.Add(name);
        }

        // Assert
        names.Count.ShouldBe(2);
        names.ShouldContain("Feature1");
        names.ShouldContain("Feature2");
    }

    [Fact]
    public async Task IsEnabledAsync_WithPercentageFilter_At100_ShouldReturnTrue()
    {
        // Arrange
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["FullRollout"] = new FeatureDefinition
            {
                Name = "FullRollout",
                Enabled = true,
                Filters = new List<FeatureFilter>
                {
                    new() { Name = "Percentage", Parameters = new Dictionary<string, object> { ["Value"] = 100 } }
                }
            }
        };
        var manager = CreateManager(features);

        // Act
        var result = await manager.IsEnabledAsync("FullRollout");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsEnabledAsync_WithPercentageFilter_At0_ShouldReturnFalse()
    {
        // Arrange
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["NoRollout"] = new FeatureDefinition
            {
                Name = "NoRollout",
                Enabled = true,
                Filters = new List<FeatureFilter>
                {
                    new() { Name = "Percentage", Parameters = new Dictionary<string, object> { ["Value"] = 0 } }
                }
            }
        };
        var manager = CreateManager(features);

        // Act
        var result = await manager.IsEnabledAsync("NoRollout");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsEnabledAsync_WithTimeWindowFilter_InsideWindow_ShouldReturnTrue()
    {
        // Arrange - use year-based window to avoid timezone issues
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["TimedFeature"] = new FeatureDefinition
            {
                Name = "TimedFeature",
                Enabled = true,
                Filters = new List<FeatureFilter>
                {
                    new()
                    {
                        Name = "TimeWindow",
                        Parameters = new Dictionary<string, object>
                        {
                            ["Start"] = "2020-01-01",
                            ["End"] = "2030-12-31"
                        }
                    }
                }
            }
        };
        var manager = CreateManager(features);

        // Act
        var result = await manager.IsEnabledAsync("TimedFeature");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsEnabledAsync_WithTimeWindowFilter_OutsideWindow_ShouldReturnFalse()
    {
        // Arrange
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["ExpiredFeature"] = new FeatureDefinition
            {
                Name = "ExpiredFeature",
                Enabled = true,
                Filters = new List<FeatureFilter>
                {
                    new()
                    {
                        Name = "TimeWindow",
                        Parameters = new Dictionary<string, object>
                        {
                            ["Start"] = DateTime.UtcNow.AddHours(-2).ToString("O"),
                            ["End"] = DateTime.UtcNow.AddHours(-1).ToString("O")
                        }
                    }
                }
            }
        };
        var manager = CreateManager(features);

        // Act
        var result = await manager.IsEnabledAsync("ExpiredFeature");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsEnabledAsync_WithTargetingFilter_MatchingUser_ShouldReturnTrue()
    {
        // Arrange
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["BetaFeature"] = new FeatureDefinition
            {
                Name = "BetaFeature",
                Enabled = true,
                Filters = new List<FeatureFilter>
                {
                    new()
                    {
                        Name = "Targeting",
                        Parameters = new Dictionary<string, object>
                        {
                            ["Users"] = new object[] { "user1", "user2" }
                        }
                    }
                }
            }
        };
        var manager = CreateManager(features);
        var context = new { UserId = "user1" };

        // Act
        var result = await manager.IsEnabledAsync("BetaFeature", context);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsEnabledAsync_WithTargetingFilter_NonMatchingUser_ShouldReturnFalse()
    {
        // Arrange
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["BetaFeature"] = new FeatureDefinition
            {
                Name = "BetaFeature",
                Enabled = true,
                Filters = new List<FeatureFilter>
                {
                    new()
                    {
                        Name = "Targeting",
                        Parameters = new Dictionary<string, object>
                        {
                            ["Users"] = new object[] { "user1", "user2" }
                        }
                    }
                }
            }
        };
        var manager = CreateManager(features);
        var context = new { UserId = "user3" };

        // Act
        var result = await manager.IsEnabledAsync("BetaFeature", context);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsEnabledAsync_WithTargetingFilter_MatchingGroup_ShouldReturnTrue()
    {
        // Arrange
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["GroupFeature"] = new FeatureDefinition
            {
                Name = "GroupFeature",
                Enabled = true,
                Filters = new List<FeatureFilter>
                {
                    new()
                    {
                        Name = "Targeting",
                        Parameters = new Dictionary<string, object>
                        {
                            ["Groups"] = new object[] { "beta-testers", "admins" }
                        }
                    }
                }
            }
        };
        var manager = CreateManager(features);
        var context = new { UserId = "user1", Groups = new[] { "beta-testers" } };

        // Act
        var result = await manager.IsEnabledAsync("GroupFeature", context);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsEnabledAsync_WithMultipleFilters_AllMustPass()
    {
        // Arrange - use year-based window to avoid timezone issues
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["ComplexFeature"] = new FeatureDefinition
            {
                Name = "ComplexFeature",
                Enabled = true,
                Filters = new List<FeatureFilter>
                {
                    new() { Name = "Percentage", Parameters = new Dictionary<string, object> { ["Value"] = 100 } },
                    new()
                    {
                        Name = "TimeWindow",
                        Parameters = new Dictionary<string, object>
                        {
                            ["Start"] = "2020-01-01",
                            ["End"] = "2030-12-31"
                        }
                    }
                }
            }
        };
        var manager = CreateManager(features);

        // Act
        var result = await manager.IsEnabledAsync("ComplexFeature");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsEnabledAsync_WithUnknownFilter_ShouldIgnoreFilter()
    {
        // Arrange
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["UnknownFilterFeature"] = new FeatureDefinition
            {
                Name = "UnknownFilterFeature",
                Enabled = true,
                Filters = new List<FeatureFilter>
                {
                    new() { Name = "UnknownFilter", Parameters = new Dictionary<string, object>() }
                }
            }
        };
        var manager = CreateManager(features);

        // Act
        var result = await manager.IsEnabledAsync("UnknownFilterFeature");

        // Assert
        result.ShouldBeTrue(); // Unknown filters are ignored, feature is enabled
    }

    [Fact]
    public async Task IsEnabledAsync_WithNoFilters_ShouldReturnEnabled()
    {
        // Arrange
        var features = new Dictionary<string, FeatureDefinition>
        {
            ["SimpleFeature"] = new FeatureDefinition
            {
                Name = "SimpleFeature",
                Enabled = true,
                Filters = new List<FeatureFilter>()
            }
        };
        var manager = CreateManager(features);

        // Act
        var result = await manager.IsEnabledAsync("SimpleFeature");

        // Assert
        result.ShouldBeTrue();
    }
}
