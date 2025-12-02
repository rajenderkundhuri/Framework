namespace Framework.Domain.BackgroundJobs;

/// <summary>
/// Represents the status of a background job
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Job is waiting to be executed
    /// </summary>
    Enqueued = 0,

    /// <summary>
    /// Job is scheduled for future execution
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// Job is currently being processed
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Job completed successfully
    /// </summary>
    Succeeded = 3,

    /// <summary>
    /// Job failed to complete
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Job was deleted/cancelled
    /// </summary>
    Deleted = 5,

    /// <summary>
    /// Job is awaiting continuation
    /// </summary>
    Awaiting = 6
}

/// <summary>
/// Represents the type of a background job
/// </summary>
public enum JobType
{
    /// <summary>
    /// Fire-and-forget job that runs once immediately
    /// </summary>
    FireAndForget = 0,

    /// <summary>
    /// Delayed job that runs once at a specified time
    /// </summary>
    Delayed = 1,

    /// <summary>
    /// Recurring job that runs on a schedule
    /// </summary>
    Recurring = 2,

    /// <summary>
    /// Continuation job that runs after another job completes
    /// </summary>
    Continuation = 3
}

/// <summary>
/// Represents priority levels for job execution
/// </summary>
public enum JobPriority
{
    /// <summary>
    /// Low priority - processed last
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority - default
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority - processed first
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority - immediate processing
    /// </summary>
    Critical = 3
}
