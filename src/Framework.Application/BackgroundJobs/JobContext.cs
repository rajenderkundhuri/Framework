namespace Framework.Application.BackgroundJobs;

/// <summary>
/// Context information captured when a job is enqueued
/// </summary>
public class JobContext
{
    /// <summary>
    /// Tenant ID at the time of job creation
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// User ID at the time of job creation
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// User name at the time of job creation
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Culture info for the job
    /// </summary>
    public string? Culture { get; set; }

    /// <summary>
    /// Additional context data
    /// </summary>
    public Dictionary<string, string> AdditionalData { get; set; } = new();

    /// <summary>
    /// Creates a job context from the current execution context
    /// </summary>
    public static JobContext CreateFromCurrent(
        Guid? tenantId,
        string? userId,
        string? userName,
        string? correlationId = null)
    {
        return new JobContext
        {
            TenantId = tenantId,
            UserId = userId,
            UserName = userName,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
            Culture = Thread.CurrentThread.CurrentCulture.Name
        };
    }
}

/// <summary>
/// Base class for job data that includes context
/// </summary>
public abstract class JobDataBase
{
    /// <summary>
    /// Job context captured at enqueue time
    /// </summary>
    public JobContext Context { get; set; } = new();
}

/// <summary>
/// Interface for accessing the current job context during job execution
/// </summary>
public interface IJobContextAccessor
{
    /// <summary>
    /// Gets or sets the current job context
    /// </summary>
    JobContext? CurrentContext { get; set; }
}

/// <summary>
/// Default implementation of IJobContextAccessor using AsyncLocal
/// </summary>
public class JobContextAccessor : IJobContextAccessor
{
    private static readonly AsyncLocal<JobContext?> _currentContext = new();

    public JobContext? CurrentContext
    {
        get => _currentContext.Value;
        set => _currentContext.Value = value;
    }
}
