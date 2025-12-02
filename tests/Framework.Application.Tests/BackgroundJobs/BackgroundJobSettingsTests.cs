using Framework.Application.BackgroundJobs;
using Shouldly;

namespace Framework.Application.Tests.BackgroundJobs;

public class BackgroundJobSettingsTests
{
    [Fact]
    public void SectionName_ShouldBeBackgroundJobs()
    {
        BackgroundJobSettings.SectionName.ShouldBe("BackgroundJobs");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Act
        var settings = new BackgroundJobSettings();

        // Assert
        settings.IsEnabled.ShouldBeTrue();
        settings.StorageConnectionString.ShouldBeNull();
        settings.StorageType.ShouldBe(JobStorageType.InMemory);
        settings.WorkerCount.ShouldBe(Environment.ProcessorCount * 2);
        settings.DefaultRetryCount.ShouldBe(3);
        settings.EnableDashboard.ShouldBeTrue();
        settings.DashboardPath.ShouldBe("/jobs");
        settings.DashboardRequireAuthentication.ShouldBeTrue();
        settings.SuccessfulJobRetentionDays.ShouldBe(1);
        settings.FailedJobRetentionDays.ShouldBe(7);
        settings.HeartbeatIntervalSeconds.ShouldBe(30);
        settings.JobTimeoutMinutes.ShouldBe(30);
    }

    [Fact]
    public void Queues_ShouldHaveDefaultConfiguration()
    {
        // Act
        var settings = new BackgroundJobSettings();

        // Assert
        settings.Queues.Count.ShouldBe(3);

        var criticalQueue = settings.Queues.FirstOrDefault(q => q.Name == "critical");
        criticalQueue.ShouldNotBeNull();
        criticalQueue.Priority.ShouldBe(3);

        var defaultQueue = settings.Queues.FirstOrDefault(q => q.Name == "default");
        defaultQueue.ShouldNotBeNull();
        defaultQueue.Priority.ShouldBe(1);

        var lowQueue = settings.Queues.FirstOrDefault(q => q.Name == "low");
        lowQueue.ShouldNotBeNull();
        lowQueue.Priority.ShouldBe(0);
    }

    [Fact]
    public void RetryDelaysInSeconds_ShouldHaveDefaultValues()
    {
        // Act
        var settings = new BackgroundJobSettings();

        // Assert
        settings.RetryDelaysInSeconds.Length.ShouldBe(5);
        settings.RetryDelaysInSeconds[0].ShouldBe(30);
        settings.RetryDelaysInSeconds[1].ShouldBe(60);
        settings.RetryDelaysInSeconds[2].ShouldBe(300);
        settings.RetryDelaysInSeconds[3].ShouldBe(900);
        settings.RetryDelaysInSeconds[4].ShouldBe(3600);
    }

    [Fact]
    public void DashboardAllowedRoles_ShouldHaveAdmin()
    {
        // Act
        var settings = new BackgroundJobSettings();

        // Assert
        settings.DashboardAllowedRoles.ShouldHaveSingleItem();
        settings.DashboardAllowedRoles.ShouldContain("Admin");
    }

    [Fact]
    public void AllProperties_CanBeModified()
    {
        // Arrange
        var settings = new BackgroundJobSettings();

        // Act
        settings.IsEnabled = false;
        settings.StorageConnectionString = "Server=localhost;Database=Jobs";
        settings.StorageType = JobStorageType.SqlServer;
        settings.WorkerCount = 10;
        settings.DefaultRetryCount = 5;
        settings.EnableDashboard = false;
        settings.DashboardPath = "/hangfire";
        settings.DashboardRequireAuthentication = false;
        settings.SuccessfulJobRetentionDays = 7;
        settings.FailedJobRetentionDays = 30;
        settings.ServerNamePrefix = "api";
        settings.HeartbeatIntervalSeconds = 60;
        settings.JobTimeoutMinutes = 60;
        settings.DashboardAllowedRoles.Add("SuperAdmin");

        // Assert
        settings.IsEnabled.ShouldBeFalse();
        settings.StorageConnectionString.ShouldBe("Server=localhost;Database=Jobs");
        settings.StorageType.ShouldBe(JobStorageType.SqlServer);
        settings.WorkerCount.ShouldBe(10);
        settings.DefaultRetryCount.ShouldBe(5);
        settings.EnableDashboard.ShouldBeFalse();
        settings.DashboardPath.ShouldBe("/hangfire");
        settings.DashboardRequireAuthentication.ShouldBeFalse();
        settings.SuccessfulJobRetentionDays.ShouldBe(7);
        settings.FailedJobRetentionDays.ShouldBe(30);
        settings.ServerNamePrefix.ShouldBe("api");
        settings.HeartbeatIntervalSeconds.ShouldBe(60);
        settings.JobTimeoutMinutes.ShouldBe(60);
        settings.DashboardAllowedRoles.ShouldContain("SuperAdmin");
    }
}

public class QueueConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new QueueConfiguration();

        // Assert
        config.Name.ShouldBe("default");
        config.Priority.ShouldBe(1);
        config.MaxConcurrentJobs.ShouldBeNull();
    }

    [Fact]
    public void AllProperties_CanBeModified()
    {
        // Arrange
        var config = new QueueConfiguration
        {
            Name = "critical",
            Priority = 10,
            MaxConcurrentJobs = 5
        };

        // Assert
        config.Name.ShouldBe("critical");
        config.Priority.ShouldBe(10);
        config.MaxConcurrentJobs.ShouldBe(5);
    }
}

public class JobStorageTypeTests
{
    [Theory]
    [InlineData(JobStorageType.InMemory, 0)]
    [InlineData(JobStorageType.SqlServer, 1)]
    [InlineData(JobStorageType.PostgreSql, 2)]
    [InlineData(JobStorageType.Redis, 3)]
    public void JobStorageType_ShouldHaveCorrectValues(JobStorageType type, int expected)
    {
        ((int)type).ShouldBe(expected);
    }
}
