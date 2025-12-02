namespace Framework.Application.BackgroundJobs;

/// <summary>
/// Marker interface for background jobs
/// </summary>
public interface IBackgroundJob
{
}

/// <summary>
/// Interface for background jobs that process data of type T
/// </summary>
/// <typeparam name="TData">Type of data the job processes</typeparam>
public interface IBackgroundJob<in TData> : IBackgroundJob where TData : class
{
    /// <summary>
    /// Executes the job with the provided data
    /// </summary>
    /// <param name="data">Job data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteAsync(TData data, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for recurring background jobs
/// </summary>
public interface IRecurringJob : IBackgroundJob
{
    /// <summary>
    /// Gets the unique identifier for this recurring job
    /// </summary>
    string JobId { get; }

    /// <summary>
    /// Gets the CRON expression for the job schedule
    /// </summary>
    string CronExpression { get; }

    /// <summary>
    /// Gets the queue name for the job (optional)
    /// </summary>
    string? Queue => null;

    /// <summary>
    /// Gets the timezone for the CRON schedule
    /// </summary>
    TimeZoneInfo TimeZone => TimeZoneInfo.Utc;

    /// <summary>
    /// Executes the recurring job
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Attribute to specify job configuration
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class BackgroundJobAttribute : Attribute
{
    /// <summary>
    /// Queue name for the job
    /// </summary>
    public string? Queue { get; set; }

    /// <summary>
    /// Maximum retry attempts on failure
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Whether the job should run in isolation (no other jobs from same queue)
    /// </summary>
    public bool DisableConcurrentExecution { get; set; }
}
