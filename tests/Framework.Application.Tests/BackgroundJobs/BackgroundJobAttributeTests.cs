using Framework.Application.BackgroundJobs;
using Shouldly;

namespace Framework.Application.Tests.BackgroundJobs;

public class BackgroundJobAttributeTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Act
        var attribute = new BackgroundJobAttribute();

        // Assert
        attribute.Queue.ShouldBeNull();
        attribute.MaxRetries.ShouldBe(3);
        attribute.TimeoutSeconds.ShouldBe(300);
        attribute.DisableConcurrentExecution.ShouldBeFalse();
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        // Act
        var attribute = new BackgroundJobAttribute
        {
            Queue = "critical",
            MaxRetries = 5,
            TimeoutSeconds = 600,
            DisableConcurrentExecution = true
        };

        // Assert
        attribute.Queue.ShouldBe("critical");
        attribute.MaxRetries.ShouldBe(5);
        attribute.TimeoutSeconds.ShouldBe(600);
        attribute.DisableConcurrentExecution.ShouldBeTrue();
    }

    [Fact]
    public void AttributeUsage_ShouldBeClassOnly()
    {
        // Arrange
        var attributeUsage = typeof(BackgroundJobAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage.ValidOn.ShouldBe(AttributeTargets.Class);
        attributeUsage.AllowMultiple.ShouldBeFalse();
    }
}

public class RecurringJobTests
{
    private class TestRecurringJob : IRecurringJob
    {
        public string JobId => "test-recurring-job";
        public string CronExpression => CronExpressions.Daily;
        public string? Queue => "default";
        public TimeZoneInfo TimeZone => TimeZoneInfo.Local;

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private class MinimalRecurringJob : IRecurringJob
    {
        public string JobId => "minimal-job";
        public string CronExpression => "* * * * *";

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void RecurringJob_ShouldImplementInterface()
    {
        // Arrange
        var job = new TestRecurringJob();

        // Assert
        job.JobId.ShouldBe("test-recurring-job");
        job.CronExpression.ShouldBe(CronExpressions.Daily);
        job.Queue.ShouldBe("default");
        job.TimeZone.ShouldBe(TimeZoneInfo.Local);
    }

    [Fact]
    public void MinimalRecurringJob_ShouldHaveDefaults()
    {
        // Arrange
        IRecurringJob job = new MinimalRecurringJob();

        // Assert
        job.JobId.ShouldBe("minimal-job");
        job.CronExpression.ShouldBe("* * * * *");
        job.Queue.ShouldBeNull(); // Default from interface
        job.TimeZone.ShouldBe(TimeZoneInfo.Utc); // Default from interface
    }

    [Fact]
    public async Task ExecuteAsync_ShouldComplete()
    {
        // Arrange
        var job = new TestRecurringJob();

        // Act & Assert (should not throw)
        await job.ExecuteAsync();
    }
}

public class BackgroundJobInterfaceTests
{
    private class TestJobData
    {
        public string? Message { get; set; }
        public int Value { get; set; }
    }

    private class TestBackgroundJob : IBackgroundJob<TestJobData>
    {
        public string? LastMessage { get; private set; }
        public int ExecutionCount { get; private set; }

        public Task ExecuteAsync(TestJobData data, CancellationToken cancellationToken = default)
        {
            LastMessage = data.Message;
            ExecutionCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task BackgroundJob_ShouldProcessData()
    {
        // Arrange
        var job = new TestBackgroundJob();
        var data = new TestJobData { Message = "Hello", Value = 42 };

        // Act
        await job.ExecuteAsync(data);

        // Assert
        job.LastMessage.ShouldBe("Hello");
        job.ExecutionCount.ShouldBe(1);
    }

    [Fact]
    public async Task BackgroundJob_ShouldHandleMultipleExecutions()
    {
        // Arrange
        var job = new TestBackgroundJob();

        // Act
        await job.ExecuteAsync(new TestJobData { Message = "First" });
        await job.ExecuteAsync(new TestJobData { Message = "Second" });
        await job.ExecuteAsync(new TestJobData { Message = "Third" });

        // Assert
        job.LastMessage.ShouldBe("Third");
        job.ExecutionCount.ShouldBe(3);
    }

    [Fact]
    public async Task BackgroundJob_ShouldRespectCancellation()
    {
        // Arrange
        var job = new CancellableBackgroundJob();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => job.ExecuteAsync(new TestJobData(), cts.Token));
    }

    private class CancellableBackgroundJob : IBackgroundJob<TestJobData>
    {
        public Task ExecuteAsync(TestJobData data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
