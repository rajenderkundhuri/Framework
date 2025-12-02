namespace Framework.Infrastructure.Persistence;

/// <summary>
/// Database provider types
/// </summary>
public enum DatabaseProvider
{
    /// <summary>
    /// MySQL/MariaDB
    /// </summary>
    MySql = 0,

    /// <summary>
    /// Microsoft SQL Server
    /// </summary>
    SqlServer = 1,

    /// <summary>
    /// PostgreSQL
    /// </summary>
    PostgreSql = 2,

    /// <summary>
    /// In-Memory (for testing)
    /// </summary>
    InMemory = 3
}

/// <summary>
/// Database configuration settings
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Database provider to use
    /// </summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.MySql;

    /// <summary>
    /// Connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Enable retry on failure for transient errors
    /// </summary>
    public bool EnableRetryOnFailure { get; set; } = true;

    /// <summary>
    /// Maximum retry count for transient errors
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Maximum retry delay in seconds
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Enable sensitive data logging (for development only)
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Enable detailed errors (for development only)
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 30;
}
