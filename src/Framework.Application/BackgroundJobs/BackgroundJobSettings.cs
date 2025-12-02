namespace Framework.Application.BackgroundJobs;

/// <summary>
/// Configuration settings for background job processing
/// </summary>
public class BackgroundJobSettings
{
    public const string SectionName = "BackgroundJobs";

    /// <summary>
    /// Whether background job processing is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Connection string for the job storage (e.g., SQL Server, Redis)
    /// </summary>
    public string? StorageConnectionString { get; set; }

    /// <summary>
    /// Type of storage to use (InMemory, SqlServer, Redis)
    /// </summary>
    public JobStorageType StorageType { get; set; } = JobStorageType.InMemory;

    /// <summary>
    /// Number of worker threads per server
    /// </summary>
    public int WorkerCount { get; set; } = Environment.ProcessorCount * 2;

    /// <summary>
    /// Queues to process with their priorities
    /// </summary>
    public List<QueueConfiguration> Queues { get; set; } = new()
    {
        new QueueConfiguration { Name = "critical", Priority = 3 },
        new QueueConfiguration { Name = "default", Priority = 1 },
        new QueueConfiguration { Name = "low", Priority = 0 }
    };

    /// <summary>
    /// Default retry count for failed jobs
    /// </summary>
    public int DefaultRetryCount { get; set; } = 3;

    /// <summary>
    /// Intervals between retries in seconds
    /// </summary>
    public int[] RetryDelaysInSeconds { get; set; } = { 30, 60, 300, 900, 3600 };

    /// <summary>
    /// Whether to enable the dashboard
    /// </summary>
    public bool EnableDashboard { get; set; } = true;

    /// <summary>
    /// Dashboard URL path
    /// </summary>
    public string DashboardPath { get; set; } = "/jobs";

    /// <summary>
    /// Whether to require authentication for the dashboard
    /// </summary>
    public bool DashboardRequireAuthentication { get; set; } = true;

    /// <summary>
    /// Roles allowed to access the dashboard
    /// </summary>
    public List<string> DashboardAllowedRoles { get; set; } = new() { "Admin" };

    /// <summary>
    /// How long to keep successful job data (in days)
    /// </summary>
    public int SuccessfulJobRetentionDays { get; set; } = 1;

    /// <summary>
    /// How long to keep failed job data (in days)
    /// </summary>
    public int FailedJobRetentionDays { get; set; } = 7;

    /// <summary>
    /// Server name prefix
    /// </summary>
    public string? ServerNamePrefix { get; set; }

    /// <summary>
    /// Interval for server heartbeat in seconds
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Timeout for job execution in minutes
    /// </summary>
    public int JobTimeoutMinutes { get; set; } = 30;
}

/// <summary>
/// Configuration for a job queue
/// </summary>
public class QueueConfiguration
{
    /// <summary>
    /// Queue name
    /// </summary>
    public string Name { get; set; } = "default";

    /// <summary>
    /// Queue priority (higher = processed first)
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Maximum concurrent jobs for this queue
    /// </summary>
    public int? MaxConcurrentJobs { get; set; }
}

/// <summary>
/// Type of storage for background jobs
/// </summary>
public enum JobStorageType
{
    /// <summary>
    /// In-memory storage (for development/testing only)
    /// </summary>
    InMemory = 0,

    /// <summary>
    /// SQL Server storage
    /// </summary>
    SqlServer = 1,

    /// <summary>
    /// PostgreSQL storage
    /// </summary>
    PostgreSql = 2,

    /// <summary>
    /// Redis storage
    /// </summary>
    Redis = 3
}
