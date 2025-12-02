using Framework.Application.HealthChecks;
using Shouldly;

namespace Framework.Application.Tests.HealthChecks;

public class HealthCheckResultTests
{
    [Fact]
    public void Healthy_ShouldCreateHealthyResult()
    {
        // Act
        var result = HealthCheckResult.Healthy("All good");

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe("All good");
        result.Exception.ShouldBeNull();
    }

    [Fact]
    public void Degraded_ShouldCreateDegradedResult()
    {
        // Act
        var result = HealthCheckResult.Degraded("Slow response");

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldBe("Slow response");
    }

    [Fact]
    public void Unhealthy_ShouldCreateUnhealthyResult()
    {
        // Arrange
        var ex = new Exception("Connection failed");

        // Act
        var result = HealthCheckResult.Unhealthy("Database down", ex);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Database down");
        result.Exception.ShouldBe(ex);
    }

    [Fact]
    public void Data_ShouldBeInitialized()
    {
        // Act
        var result = new HealthCheckResult();

        // Assert
        result.Data.ShouldNotBeNull();
        result.Data.ShouldBeEmpty();
    }

    [Fact]
    public void Data_ShouldAllowAddingEntries()
    {
        // Arrange
        var result = HealthCheckResult.Healthy();

        // Act
        result.Data["Key1"] = "Value1";
        result.Data["Key2"] = 42;

        // Assert
        result.Data.Count.ShouldBe(2);
        result.Data["Key1"].ShouldBe("Value1");
        result.Data["Key2"].ShouldBe(42);
    }
}

public class HealthStatusTests
{
    [Fact]
    public void HealthStatus_ShouldHaveExpectedValues()
    {
        ((int)HealthStatus.Healthy).ShouldBe(0);
        ((int)HealthStatus.Degraded).ShouldBe(1);
        ((int)HealthStatus.Unhealthy).ShouldBe(2);
    }
}

public class HealthCheckSettingsTests
{
    [Fact]
    public void SectionName_ShouldBeHealthChecks()
    {
        HealthCheckSettings.SectionName.ShouldBe("HealthChecks");
    }

    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var settings = new HealthCheckSettings();

        // Assert
        settings.Enabled.ShouldBeTrue();
        settings.Path.ShouldBe("/health");
        settings.DetailedPath.ShouldBe("/health/details");
        settings.IncludeDetails.ShouldBeTrue();
        settings.TimeoutSeconds.ShouldBe(30);
        settings.CacheDurationSeconds.ShouldBe(5);
        settings.Tags.ShouldBeEmpty();
    }
}

public class HealthReportTests
{
    [Fact]
    public void NewReport_ShouldHaveDefaults()
    {
        // Act
        var report = new HealthReport();

        // Assert
        report.Entries.ShouldNotBeNull();
        report.Entries.ShouldBeEmpty();
        report.Timestamp.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public void Report_ShouldAllowAddingEntries()
    {
        // Arrange
        var report = new HealthReport
        {
            Status = HealthStatus.Healthy,
            TotalDuration = TimeSpan.FromMilliseconds(100)
        };

        // Act
        report.Entries["Database"] = HealthCheckResult.Healthy();
        report.Entries["Cache"] = HealthCheckResult.Healthy();

        // Assert
        report.Entries.Count.ShouldBe(2);
        report.Status.ShouldBe(HealthStatus.Healthy);
    }
}
