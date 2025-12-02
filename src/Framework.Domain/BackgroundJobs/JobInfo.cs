namespace Framework.Domain.BackgroundJobs;

/// <summary>
/// Contains information about a scheduled or executed job
/// </summary>
public class JobInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Queue { get; set; }
    public JobType Type { get; set; }
    public JobStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CronExpression { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? JobData { get; set; }
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Duration of the job execution in milliseconds (set explicitly or calculated from StartedAt/CompletedAt)
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Whether the job has completed (successfully or failed)
    /// </summary>
    public bool IsCompleted => Status is JobStatus.Succeeded or JobStatus.Failed or JobStatus.Deleted;
}

/// <summary>
/// Filter for querying jobs
/// </summary>
public class JobFilter
{
    public JobStatus? Status { get; set; }
    public JobType? Type { get; set; }
    public string? Queue { get; set; }
    public string? NameContains { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public Guid? TenantId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Result of a paginated job query
/// </summary>
public class JobQueryResult
{
    public IReadOnlyList<JobInfo> Jobs { get; set; } = Array.Empty<JobInfo>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}
