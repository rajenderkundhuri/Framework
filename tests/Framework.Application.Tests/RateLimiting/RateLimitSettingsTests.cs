using Framework.Application.RateLimiting;
using Shouldly;

namespace Framework.Application.Tests.RateLimiting;

public class RateLimitSettingsTests
{
    [Fact]
    public void SectionName_ShouldBeRateLimiting()
    {
        RateLimitSettings.SectionName.ShouldBe("RateLimiting");
    }

    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var settings = new RateLimitSettings();

        // Assert
        settings.Enabled.ShouldBeTrue();
        settings.DefaultPolicy.ShouldNotBeNull();
        settings.Policies.ShouldNotBeNull();
        settings.Policies.ShouldBeEmpty();
        settings.WhitelistedIps.ShouldBeEmpty();
        settings.WhitelistedClients.ShouldBeEmpty();
        settings.IncludeHeaders.ShouldBeTrue();
        settings.StatusCode.ShouldBe(429);
        settings.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        // Act
        var settings = new RateLimitSettings
        {
            Enabled = false,
            IncludeHeaders = false,
            StatusCode = 503,
            Message = "Custom message",
            WhitelistedIps = new List<string> { "127.0.0.1" },
            WhitelistedClients = new List<string> { "trusted-client" }
        };

        // Assert
        settings.Enabled.ShouldBeFalse();
        settings.IncludeHeaders.ShouldBeFalse();
        settings.StatusCode.ShouldBe(503);
        settings.Message.ShouldBe("Custom message");
        settings.WhitelistedIps.ShouldContain("127.0.0.1");
        settings.WhitelistedClients.ShouldContain("trusted-client");
    }
}

public class RateLimitPolicyTests
{
    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var policy = new RateLimitPolicy();

        // Assert
        policy.PermitLimit.ShouldBe(100);
        policy.WindowSeconds.ShouldBe(60);
        policy.QueueLimit.ShouldBe(0);
        policy.SlidingWindow.ShouldBeTrue();
        policy.SegmentsPerWindow.ShouldBe(4);
        policy.LimitBy.ShouldBe(RateLimitBy.Ip);
        policy.AutoReplenishment.ShouldBeTrue();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        // Act
        var policy = new RateLimitPolicy
        {
            PermitLimit = 1000,
            WindowSeconds = 3600,
            QueueLimit = 10,
            SlidingWindow = false,
            SegmentsPerWindow = 8,
            LimitBy = RateLimitBy.User,
            AutoReplenishment = false
        };

        // Assert
        policy.PermitLimit.ShouldBe(1000);
        policy.WindowSeconds.ShouldBe(3600);
        policy.QueueLimit.ShouldBe(10);
        policy.SlidingWindow.ShouldBeFalse();
        policy.SegmentsPerWindow.ShouldBe(8);
        policy.LimitBy.ShouldBe(RateLimitBy.User);
        policy.AutoReplenishment.ShouldBeFalse();
    }
}

public class RateLimitByTests
{
    [Fact]
    public void RateLimitBy_ShouldHaveExpectedValues()
    {
        ((int)RateLimitBy.Ip).ShouldBe(0);
        ((int)RateLimitBy.User).ShouldBe(1);
        ((int)RateLimitBy.Client).ShouldBe(2);
        ((int)RateLimitBy.Endpoint).ShouldBe(3);
        ((int)RateLimitBy.Global).ShouldBe(4);
    }
}

public class RateLimitAttributeTests
{
    [Fact]
    public void DefaultConstructor_ShouldHaveDefaults()
    {
        // Act
        var attr = new RateLimitAttribute();

        // Assert
        attr.Policy.ShouldBeNull();
        attr.PermitLimit.ShouldBe(-1);
        attr.WindowSeconds.ShouldBe(-1);
    }

    [Fact]
    public void Constructor_WithPolicy_ShouldSetPolicy()
    {
        // Act
        var attr = new RateLimitAttribute("api-limit");

        // Assert
        attr.Policy.ShouldBe("api-limit");
    }

    [Fact]
    public void Constructor_WithLimits_ShouldSetLimits()
    {
        // Act
        var attr = new RateLimitAttribute(100, 60);

        // Assert
        attr.PermitLimit.ShouldBe(100);
        attr.WindowSeconds.ShouldBe(60);
    }
}

public class DisableRateLimitAttributeTests
{
    [Fact]
    public void CanBeInstantiated()
    {
        // Act
        var attr = new DisableRateLimitAttribute();

        // Assert
        attr.ShouldNotBeNull();
    }
}
