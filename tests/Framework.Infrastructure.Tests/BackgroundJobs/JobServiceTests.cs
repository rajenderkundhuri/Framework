using Framework.Application.BackgroundJobs;
using Framework.Domain.BackgroundJobs;
using Shouldly;

namespace Framework.Infrastructure.Tests.BackgroundJobs;

public class JobStatisticsTests
{
    [Fact]
    public void JobStatistics_DefaultValues_ShouldBeZero()
    {
        // Act
        var stats = new JobStatistics();

        // Assert
        stats.EnqueuedCount.ShouldBe(0);
        stats.ScheduledCount.ShouldBe(0);
        stats.ProcessingCount.ShouldBe(0);
        stats.SucceededCount.ShouldBe(0);
        stats.FailedCount.ShouldBe(0);
        stats.DeletedCount.ShouldBe(0);
        stats.RecurringCount.ShouldBe(0);
        stats.ServerCount.ShouldBe(0);
        stats.QueueCounts.ShouldNotBeNull();
        stats.QueueCounts.ShouldBeEmpty();
    }

    [Fact]
    public void JobStatistics_AllProperties_CanBeSet()
    {
        // Act
        var stats = new JobStatistics
        {
            EnqueuedCount = 10,
            ScheduledCount = 5,
            ProcessingCount = 2,
            SucceededCount = 100,
            FailedCount = 3,
            DeletedCount = 15,
            RecurringCount = 8,
            ServerCount = 4,
            QueueCounts = new Dictionary<string, long>
            {
                { "default", 10 },
                { "critical", 2 }
            }
        };

        // Assert
        stats.EnqueuedCount.ShouldBe(10);
        stats.ScheduledCount.ShouldBe(5);
        stats.ProcessingCount.ShouldBe(2);
        stats.SucceededCount.ShouldBe(100);
        stats.FailedCount.ShouldBe(3);
        stats.DeletedCount.ShouldBe(15);
        stats.RecurringCount.ShouldBe(8);
        stats.ServerCount.ShouldBe(4);
        stats.QueueCounts.Count.ShouldBe(2);
        stats.QueueCounts["default"].ShouldBe(10);
        stats.QueueCounts["critical"].ShouldBe(2);
    }
}

public class JobServiceInterfaceTests
{
    [Fact]
    public void IJobService_ShouldDefineAllMethods()
    {
        // Verify interface has all expected methods
        var methods = typeof(IJobService).GetMethods();

        methods.ShouldContain(m => m.Name == "Enqueue" && m.IsGenericMethod);
        methods.ShouldContain(m => m.Name == "Schedule" && m.IsGenericMethod);
        methods.ShouldContain(m => m.Name == "AddOrUpdateRecurring" && m.IsGenericMethod);
        methods.ShouldContain(m => m.Name == "RemoveRecurring");
        methods.ShouldContain(m => m.Name == "TriggerRecurring");
        methods.ShouldContain(m => m.Name == "ContinueWith");
        methods.ShouldContain(m => m.Name == "Delete");
        methods.ShouldContain(m => m.Name == "Requeue");
        methods.ShouldContain(m => m.Name == "GetJob");
        methods.ShouldContain(m => m.Name == "GetJobs");
        methods.ShouldContain(m => m.Name == "GetStatistics");
    }
}

// Mock job service for testing
public class MockJobService : IJobService
{
    public List<(string Type, object? Data)> EnqueuedJobs { get; } = new();
    public List<(string Type, object? Data, TimeSpan Delay)> ScheduledJobs { get; } = new();
    public List<string> RecurringJobs { get; } = new();
    public List<string> DeletedJobs { get; } = new();
    public List<string> RequeuedJobs { get; } = new();
    private int _jobCounter;

    public string Enqueue<TJob, TData>(TData data, string? queue = null)
        where TJob : IBackgroundJob<TData>
        where TData : class
    {
        var jobId = $"job-{++_jobCounter}";
        EnqueuedJobs.Add((typeof(TJob).Name, data));
        return jobId;
    }

    public string Enqueue(System.Linq.Expressions.Expression<Action> methodCall)
    {
        return $"job-{++_jobCounter}";
    }

    public string Enqueue(System.Linq.Expressions.Expression<Func<Task>> methodCall)
    {
        return $"job-{++_jobCounter}";
    }

    public string Schedule<TJob, TData>(TData data, TimeSpan delay, string? queue = null)
        where TJob : IBackgroundJob<TData>
        where TData : class
    {
        var jobId = $"job-{++_jobCounter}";
        ScheduledJobs.Add((typeof(TJob).Name, data, delay));
        return jobId;
    }

    public string Schedule<TJob, TData>(TData data, DateTimeOffset enqueueAt, string? queue = null)
        where TJob : IBackgroundJob<TData>
        where TData : class
    {
        var delay = enqueueAt - DateTimeOffset.UtcNow;
        return Schedule<TJob, TData>(data, delay, queue);
    }

    public string Schedule(System.Linq.Expressions.Expression<Action> methodCall, TimeSpan delay)
    {
        return $"job-{++_jobCounter}";
    }

    public string Schedule(System.Linq.Expressions.Expression<Func<Task>> methodCall, TimeSpan delay)
    {
        return $"job-{++_jobCounter}";
    }

    public string Schedule(System.Linq.Expressions.Expression<Action> methodCall, DateTimeOffset enqueueAt)
    {
        return $"job-{++_jobCounter}";
    }

    public void AddOrUpdateRecurring<TJob>() where TJob : IRecurringJob
    {
        RecurringJobs.Add(typeof(TJob).Name);
    }

    public void AddOrUpdateRecurring(string jobId, System.Linq.Expressions.Expression<Action> methodCall,
        string cronExpression, TimeZoneInfo? timeZone = null, string? queue = null)
    {
        RecurringJobs.Add(jobId);
    }

    public void AddOrUpdateRecurring(string jobId, System.Linq.Expressions.Expression<Func<Task>> methodCall,
        string cronExpression, TimeZoneInfo? timeZone = null, string? queue = null)
    {
        RecurringJobs.Add(jobId);
    }

    public void RemoveRecurring(string jobId)
    {
        RecurringJobs.Remove(jobId);
    }

    public void TriggerRecurring(string jobId)
    {
        // No-op for mock
    }

    public string ContinueWith(string parentJobId, System.Linq.Expressions.Expression<Action> methodCall)
    {
        return $"continuation-{++_jobCounter}";
    }

    public string ContinueWith(string parentJobId, System.Linq.Expressions.Expression<Func<Task>> methodCall)
    {
        return $"continuation-{++_jobCounter}";
    }

    public bool Delete(string jobId)
    {
        DeletedJobs.Add(jobId);
        return true;
    }

    public bool Requeue(string jobId)
    {
        RequeuedJobs.Add(jobId);
        return true;
    }

    public JobInfo? GetJob(string jobId)
    {
        return new JobInfo
        {
            Id = jobId,
            Name = "MockJob",
            Status = JobStatus.Succeeded
        };
    }

    public JobQueryResult GetJobs(JobFilter filter)
    {
        return new JobQueryResult
        {
            Jobs = new List<JobInfo>(),
            TotalCount = 0,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public JobStatistics GetStatistics()
    {
        return new JobStatistics
        {
            EnqueuedCount = EnqueuedJobs.Count,
            RecurringCount = RecurringJobs.Count
        };
    }
}

public class MockJobServiceTests
{
    private class TestJobData
    {
        public string? Value { get; set; }
    }

    private class TestJob : IBackgroundJob<TestJobData>
    {
        public Task ExecuteAsync(TestJobData data, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    [Fact]
    public void Enqueue_ShouldTrackJob()
    {
        // Arrange
        var service = new MockJobService();
        var data = new TestJobData { Value = "test" };

        // Act
        var jobId = service.Enqueue<TestJob, TestJobData>(data);

        // Assert
        jobId.ShouldNotBeNull();
        service.EnqueuedJobs.ShouldHaveSingleItem();
        service.EnqueuedJobs[0].Type.ShouldBe("TestJob");
    }

    [Fact]
    public void Schedule_ShouldTrackJobWithDelay()
    {
        // Arrange
        var service = new MockJobService();
        var data = new TestJobData { Value = "test" };
        var delay = TimeSpan.FromMinutes(5);

        // Act
        var jobId = service.Schedule<TestJob, TestJobData>(data, delay);

        // Assert
        jobId.ShouldNotBeNull();
        service.ScheduledJobs.ShouldHaveSingleItem();
        service.ScheduledJobs[0].Delay.ShouldBe(delay);
    }

    [Fact]
    public void Delete_ShouldTrackDeletion()
    {
        // Arrange
        var service = new MockJobService();

        // Act
        var result = service.Delete("job-123");

        // Assert
        result.ShouldBeTrue();
        service.DeletedJobs.ShouldContain("job-123");
    }

    [Fact]
    public void Requeue_ShouldTrackRequeue()
    {
        // Arrange
        var service = new MockJobService();

        // Act
        var result = service.Requeue("job-456");

        // Assert
        result.ShouldBeTrue();
        service.RequeuedJobs.ShouldContain("job-456");
    }

    [Fact]
    public void GetStatistics_ShouldReturnCorrectCounts()
    {
        // Arrange
        var service = new MockJobService();
        service.Enqueue<TestJob, TestJobData>(new TestJobData());
        service.Enqueue<TestJob, TestJobData>(new TestJobData());
        service.AddOrUpdateRecurring("recurring-1", () => Console.WriteLine("test"), "* * * * *");

        // Act
        var stats = service.GetStatistics();

        // Assert
        stats.EnqueuedCount.ShouldBe(2);
        stats.RecurringCount.ShouldBe(1);
    }

    [Fact]
    public void GetJob_ShouldReturnJobInfo()
    {
        // Arrange
        var service = new MockJobService();

        // Act
        var job = service.GetJob("job-123");

        // Assert
        job.ShouldNotBeNull();
        job.Id.ShouldBe("job-123");
        job.Name.ShouldBe("MockJob");
        job.Status.ShouldBe(JobStatus.Succeeded);
    }

    [Fact]
    public void GetJobs_ShouldReturnQueryResult()
    {
        // Arrange
        var service = new MockJobService();
        var filter = new JobFilter { PageNumber = 2, PageSize = 10 };

        // Act
        var result = service.GetJobs(filter);

        // Assert
        result.ShouldNotBeNull();
        result.PageNumber.ShouldBe(2);
        result.PageSize.ShouldBe(10);
        result.Jobs.ShouldBeEmpty();
    }

    [Fact]
    public void ContinueWith_ShouldReturnContinuationJobId()
    {
        // Arrange
        var service = new MockJobService();

        // Act
        var continuationId = service.ContinueWith("parent-job", () => Console.WriteLine("continuation"));

        // Assert
        continuationId.ShouldStartWith("continuation-");
    }

    [Fact]
    public void AddOrUpdateRecurring_ShouldTrackRecurringJob()
    {
        // Arrange
        var service = new MockJobService();

        // Act
        service.AddOrUpdateRecurring("daily-job", () => Console.WriteLine("daily"), CronExpressions.Daily);

        // Assert
        service.RecurringJobs.ShouldContain("daily-job");
    }

    [Fact]
    public void RemoveRecurring_ShouldRemoveJob()
    {
        // Arrange
        var service = new MockJobService();
        service.AddOrUpdateRecurring("to-remove", () => Console.WriteLine("test"), CronExpressions.Hourly);
        service.RecurringJobs.ShouldContain("to-remove");

        // Act
        service.RemoveRecurring("to-remove");

        // Assert
        service.RecurringJobs.ShouldNotContain("to-remove");
    }
}
