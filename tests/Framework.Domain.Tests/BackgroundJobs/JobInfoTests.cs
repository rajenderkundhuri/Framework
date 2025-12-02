using Framework.Domain.BackgroundJobs;
using Shouldly;

namespace Framework.Domain.Tests.BackgroundJobs;

public class JobInfoTests
{
    [Fact]
    public void JobInfo_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var jobInfo = new JobInfo();

        // Assert
        jobInfo.Id.ShouldBe(string.Empty);
        jobInfo.Name.ShouldBe(string.Empty);
        jobInfo.Queue.ShouldBeNull();
        jobInfo.Status.ShouldBe(JobStatus.Enqueued);
        jobInfo.Priority.ShouldBe(JobPriority.Low); // Default enum value is 0 (Low)
        jobInfo.ErrorMessage.ShouldBeNull();
        jobInfo.DurationMs.ShouldBeNull();
        jobInfo.IsCompleted.ShouldBeFalse();
    }

    [Theory]
    [InlineData(JobStatus.Succeeded, true)]
    [InlineData(JobStatus.Failed, true)]
    [InlineData(JobStatus.Deleted, true)]
    [InlineData(JobStatus.Enqueued, false)]
    [InlineData(JobStatus.Scheduled, false)]
    [InlineData(JobStatus.Processing, false)]
    [InlineData(JobStatus.Awaiting, false)]
    public void IsCompleted_ShouldReturnCorrectValue(JobStatus status, bool expected)
    {
        // Arrange
        var jobInfo = new JobInfo { Status = status };

        // Assert
        jobInfo.IsCompleted.ShouldBe(expected);
    }

    [Fact]
    public void JobInfo_WithAllProperties_ShouldSetCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var jobInfo = new JobInfo
        {
            Id = "job-123",
            Name = "TestJob",
            Queue = "default",
            Type = JobType.FireAndForget,
            Status = JobStatus.Succeeded,
            Priority = JobPriority.High,
            CreatedAt = now.AddMinutes(-10),
            ScheduledAt = now.AddMinutes(-5),
            StartedAt = now.AddMinutes(-2),
            CompletedAt = now,
            CronExpression = "* * * * *",
            RetryCount = 2,
            ErrorMessage = null,
            JobData = "{\"key\":\"value\"}",
            TenantId = tenantId,
            DurationMs = 5000
        };

        // Assert
        jobInfo.Id.ShouldBe("job-123");
        jobInfo.Name.ShouldBe("TestJob");
        jobInfo.Queue.ShouldBe("default");
        jobInfo.Type.ShouldBe(JobType.FireAndForget);
        jobInfo.Status.ShouldBe(JobStatus.Succeeded);
        jobInfo.Priority.ShouldBe(JobPriority.High);
        jobInfo.RetryCount.ShouldBe(2);
        jobInfo.TenantId.ShouldBe(tenantId);
        jobInfo.DurationMs.ShouldBe(5000);
        jobInfo.IsCompleted.ShouldBeTrue();
    }
}

public class JobStatusTests
{
    [Theory]
    [InlineData(JobStatus.Enqueued, 0)]
    [InlineData(JobStatus.Scheduled, 1)]
    [InlineData(JobStatus.Processing, 2)]
    [InlineData(JobStatus.Succeeded, 3)]
    [InlineData(JobStatus.Failed, 4)]
    [InlineData(JobStatus.Deleted, 5)]
    [InlineData(JobStatus.Awaiting, 6)]
    public void JobStatus_ShouldHaveCorrectValues(JobStatus status, int expectedValue)
    {
        ((int)status).ShouldBe(expectedValue);
    }
}

public class JobTypeTests
{
    [Theory]
    [InlineData(JobType.FireAndForget, 0)]
    [InlineData(JobType.Delayed, 1)]
    [InlineData(JobType.Recurring, 2)]
    [InlineData(JobType.Continuation, 3)]
    public void JobType_ShouldHaveCorrectValues(JobType type, int expectedValue)
    {
        ((int)type).ShouldBe(expectedValue);
    }
}

public class JobPriorityTests
{
    [Theory]
    [InlineData(JobPriority.Low, 0)]
    [InlineData(JobPriority.Normal, 1)]
    [InlineData(JobPriority.High, 2)]
    [InlineData(JobPriority.Critical, 3)]
    public void JobPriority_ShouldHaveCorrectValues(JobPriority priority, int expectedValue)
    {
        ((int)priority).ShouldBe(expectedValue);
    }
}

public class JobFilterTests
{
    [Fact]
    public void JobFilter_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var filter = new JobFilter();

        // Assert
        filter.Status.ShouldBeNull();
        filter.Type.ShouldBeNull();
        filter.Queue.ShouldBeNull();
        filter.NameContains.ShouldBeNull();
        filter.FromDate.ShouldBeNull();
        filter.ToDate.ShouldBeNull();
        filter.TenantId.ShouldBeNull();
        filter.PageNumber.ShouldBe(1);
        filter.PageSize.ShouldBe(20);
    }

    [Fact]
    public void JobFilter_WithAllProperties_ShouldSetCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;

        // Act
        var filter = new JobFilter
        {
            Status = JobStatus.Failed,
            Type = JobType.Recurring,
            Queue = "critical",
            NameContains = "Report",
            FromDate = fromDate,
            ToDate = toDate,
            TenantId = tenantId,
            PageNumber = 3,
            PageSize = 50
        };

        // Assert
        filter.Status.ShouldBe(JobStatus.Failed);
        filter.Type.ShouldBe(JobType.Recurring);
        filter.Queue.ShouldBe("critical");
        filter.NameContains.ShouldBe("Report");
        filter.FromDate.ShouldBe(fromDate);
        filter.ToDate.ShouldBe(toDate);
        filter.TenantId.ShouldBe(tenantId);
        filter.PageNumber.ShouldBe(3);
        filter.PageSize.ShouldBe(50);
    }
}

public class JobQueryResultTests
{
    [Fact]
    public void JobQueryResult_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var result = new JobQueryResult();

        // Assert
        result.Jobs.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.PageNumber.ShouldBe(0);
        result.PageSize.ShouldBe(0);
    }

    [Theory]
    [InlineData(100, 20, 5)]
    [InlineData(95, 20, 5)]
    [InlineData(0, 20, 0)]
    [InlineData(21, 20, 2)]
    [InlineData(20, 20, 1)]
    public void TotalPages_ShouldCalculateCorrectly(int totalCount, int pageSize, int expectedPages)
    {
        // Arrange
        var result = new JobQueryResult
        {
            TotalCount = totalCount,
            PageSize = pageSize
        };

        // Assert
        result.TotalPages.ShouldBe(expectedPages);
    }

    [Theory]
    [InlineData(1, 5, false)]
    [InlineData(2, 5, true)]
    [InlineData(5, 5, true)]
    public void HasPreviousPage_ShouldReturnCorrectly(int pageNumber, int totalPages, bool expected)
    {
        // Arrange
        var result = new JobQueryResult
        {
            PageNumber = pageNumber,
            PageSize = 20,
            TotalCount = totalPages * 20
        };

        // Assert
        result.HasPreviousPage.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, 5, true)]
    [InlineData(4, 5, true)]
    [InlineData(5, 5, false)]
    public void HasNextPage_ShouldReturnCorrectly(int pageNumber, int totalPages, bool expected)
    {
        // Arrange
        var result = new JobQueryResult
        {
            PageNumber = pageNumber,
            PageSize = 20,
            TotalCount = totalPages * 20
        };

        // Assert
        result.HasNextPage.ShouldBe(expected);
    }
}
