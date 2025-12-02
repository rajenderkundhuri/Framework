namespace Framework.Application.HealthChecks;

/// <summary>
/// Interface for custom health checks
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    /// Name of the health check
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Tags for categorization
    /// </summary>
    IEnumerable<string> Tags { get; }

    /// <summary>
    /// Performs the health check
    /// </summary>
    Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a health check
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// Health status
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Description of the check result
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Exception if unhealthy
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Additional data
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Duration of the check
    /// </summary>
    public TimeSpan Duration { get; set; }

    public static HealthCheckResult Healthy(string? description = null)
        => new() { Status = HealthStatus.Healthy, Description = description };

    public static HealthCheckResult Degraded(string? description = null, Exception? exception = null)
        => new() { Status = HealthStatus.Degraded, Description = description, Exception = exception };

    public static HealthCheckResult Unhealthy(string? description = null, Exception? exception = null)
        => new() { Status = HealthStatus.Unhealthy, Description = description, Exception = exception };
}

/// <summary>
/// Health status values
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Service is healthy
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// Service is degraded but operational
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// Service is unhealthy
    /// </summary>
    Unhealthy = 2
}

/// <summary>
/// Settings for health checks
/// </summary>
public class HealthCheckSettings
{
    public const string SectionName = "HealthChecks";

    /// <summary>
    /// Whether health checks are enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Path for health endpoint
    /// </summary>
    public string Path { get; set; } = "/health";

    /// <summary>
    /// Path for detailed health endpoint
    /// </summary>
    public string DetailedPath { get; set; } = "/health/details";

    /// <summary>
    /// Whether to include details in response
    /// </summary>
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Timeout for health checks in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Cache duration in seconds (0 = no cache)
    /// </summary>
    public int CacheDurationSeconds { get; set; } = 5;

    /// <summary>
    /// Tags to filter health checks
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Aggregate health report
/// </summary>
public class HealthReport
{
    /// <summary>
    /// Overall status
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Total duration
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// Individual check results
    /// </summary>
    public Dictionary<string, HealthCheckResult> Entries { get; set; } = new();

    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
