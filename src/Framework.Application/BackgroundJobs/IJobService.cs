using System.Linq.Expressions;
using Framework.Domain.BackgroundJobs;

namespace Framework.Application.BackgroundJobs;

/// <summary>
/// Service for managing background jobs
/// </summary>
public interface IJobService
{
    /// <summary>
    /// Enqueues a fire-and-forget job for immediate execution
    /// </summary>
    /// <typeparam name="TJob">Type of the job</typeparam>
    /// <typeparam name="TData">Type of the job data</typeparam>
    /// <param name="data">Job data</param>
    /// <param name="queue">Optional queue name</param>
    /// <returns>Job ID</returns>
    string Enqueue<TJob, TData>(TData data, string? queue = null)
        where TJob : IBackgroundJob<TData>
        where TData : class;

    /// <summary>
    /// Enqueues a fire-and-forget job for immediate execution using an expression
    /// </summary>
    /// <param name="methodCall">Expression representing the method to call</param>
    /// <returns>Job ID</returns>
    string Enqueue(Expression<Action> methodCall);

    /// <summary>
    /// Enqueues a fire-and-forget async job for immediate execution using an expression
    /// </summary>
    /// <param name="methodCall">Expression representing the async method to call</param>
    /// <returns>Job ID</returns>
    string Enqueue(Expression<Func<Task>> methodCall);

    /// <summary>
    /// Schedules a job for delayed execution
    /// </summary>
    /// <typeparam name="TJob">Type of the job</typeparam>
    /// <typeparam name="TData">Type of the job data</typeparam>
    /// <param name="data">Job data</param>
    /// <param name="delay">Delay before execution</param>
    /// <param name="queue">Optional queue name</param>
    /// <returns>Job ID</returns>
    string Schedule<TJob, TData>(TData data, TimeSpan delay, string? queue = null)
        where TJob : IBackgroundJob<TData>
        where TData : class;

    /// <summary>
    /// Schedules a job for execution at a specific time
    /// </summary>
    /// <typeparam name="TJob">Type of the job</typeparam>
    /// <typeparam name="TData">Type of the job data</typeparam>
    /// <param name="data">Job data</param>
    /// <param name="enqueueAt">Time to execute the job</param>
    /// <param name="queue">Optional queue name</param>
    /// <returns>Job ID</returns>
    string Schedule<TJob, TData>(TData data, DateTimeOffset enqueueAt, string? queue = null)
        where TJob : IBackgroundJob<TData>
        where TData : class;

    /// <summary>
    /// Schedules a delayed job using an expression
    /// </summary>
    /// <param name="methodCall">Expression representing the method to call</param>
    /// <param name="delay">Delay before execution</param>
    /// <returns>Job ID</returns>
    string Schedule(Expression<Action> methodCall, TimeSpan delay);

    /// <summary>
    /// Schedules a delayed async job using an expression
    /// </summary>
    /// <param name="methodCall">Expression representing the async method to call</param>
    /// <param name="delay">Delay before execution</param>
    /// <returns>Job ID</returns>
    string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay);

    /// <summary>
    /// Schedules a job for a specific time using an expression
    /// </summary>
    /// <param name="methodCall">Expression representing the method to call</param>
    /// <param name="enqueueAt">Time to execute the job</param>
    /// <returns>Job ID</returns>
    string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt);

    /// <summary>
    /// Adds or updates a recurring job
    /// </summary>
    /// <typeparam name="TJob">Type of the recurring job</typeparam>
    void AddOrUpdateRecurring<TJob>() where TJob : IRecurringJob;

    /// <summary>
    /// Adds or updates a recurring job with custom parameters
    /// </summary>
    /// <param name="jobId">Unique identifier for the recurring job</param>
    /// <param name="methodCall">Expression representing the method to call</param>
    /// <param name="cronExpression">CRON expression for the schedule</param>
    /// <param name="timeZone">Timezone for the schedule</param>
    /// <param name="queue">Optional queue name</param>
    void AddOrUpdateRecurring(
        string jobId,
        Expression<Action> methodCall,
        string cronExpression,
        TimeZoneInfo? timeZone = null,
        string? queue = null);

    /// <summary>
    /// Adds or updates a recurring async job with custom parameters
    /// </summary>
    /// <param name="jobId">Unique identifier for the recurring job</param>
    /// <param name="methodCall">Expression representing the async method to call</param>
    /// <param name="cronExpression">CRON expression for the schedule</param>
    /// <param name="timeZone">Timezone for the schedule</param>
    /// <param name="queue">Optional queue name</param>
    void AddOrUpdateRecurring(
        string jobId,
        Expression<Func<Task>> methodCall,
        string cronExpression,
        TimeZoneInfo? timeZone = null,
        string? queue = null);

    /// <summary>
    /// Removes a recurring job
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    void RemoveRecurring(string jobId);

    /// <summary>
    /// Triggers a recurring job to run immediately
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    void TriggerRecurring(string jobId);

    /// <summary>
    /// Creates a continuation job that runs after the parent job completes
    /// </summary>
    /// <param name="parentJobId">ID of the parent job</param>
    /// <param name="methodCall">Expression representing the method to call</param>
    /// <returns>Continuation job ID</returns>
    string ContinueWith(string parentJobId, Expression<Action> methodCall);

    /// <summary>
    /// Creates a continuation async job that runs after the parent job completes
    /// </summary>
    /// <param name="parentJobId">ID of the parent job</param>
    /// <param name="methodCall">Expression representing the async method to call</param>
    /// <returns>Continuation job ID</returns>
    string ContinueWith(string parentJobId, Expression<Func<Task>> methodCall);

    /// <summary>
    /// Deletes a job
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <returns>True if job was deleted</returns>
    bool Delete(string jobId);

    /// <summary>
    /// Requeues a failed job for retry
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <returns>True if job was requeued</returns>
    bool Requeue(string jobId);

    /// <summary>
    /// Gets information about a specific job
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <returns>Job information or null if not found</returns>
    JobInfo? GetJob(string jobId);

    /// <summary>
    /// Queries jobs with filtering and pagination
    /// </summary>
    /// <param name="filter">Job filter</param>
    /// <returns>Query result with jobs</returns>
    JobQueryResult GetJobs(JobFilter filter);

    /// <summary>
    /// Gets statistics about job queues
    /// </summary>
    /// <returns>Queue statistics</returns>
    JobStatistics GetStatistics();
}

/// <summary>
/// Statistics about background job queues
/// </summary>
public class JobStatistics
{
    public long EnqueuedCount { get; set; }
    public long ScheduledCount { get; set; }
    public long ProcessingCount { get; set; }
    public long SucceededCount { get; set; }
    public long FailedCount { get; set; }
    public long DeletedCount { get; set; }
    public long RecurringCount { get; set; }
    public Dictionary<string, long> QueueCounts { get; set; } = new();
    public int ServerCount { get; set; }
}
