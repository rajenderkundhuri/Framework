namespace Framework.Application.Outbox;

/// <summary>
/// Interface for outbox pattern service
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Adds a message to the outbox
    /// </summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple messages to the outbox
    /// </summary>
    Task AddRangeAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending messages
    /// </summary>
    Task<IEnumerable<OutboxMessage>> GetPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as processed
    /// </summary>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as failed
    /// </summary>
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries failed messages
    /// </summary>
    Task<int> RetryFailedAsync(int maxRetries = 3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old processed messages
    /// </summary>
    Task<int> CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Outbox message entity
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Message ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Message type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Serialized message payload
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// When the message was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the message was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Processing status
    /// </summary>
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

    /// <summary>
    /// Number of processing attempts
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Last error message
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Creates an outbox message from an object
    /// </summary>
    public static OutboxMessage Create<T>(T payload, string? correlationId = null, Guid? tenantId = null)
    {
        return new OutboxMessage
        {
            Type = typeof(T).AssemblyQualifiedName ?? typeof(T).Name,
            Payload = System.Text.Json.JsonSerializer.Serialize(payload),
            CorrelationId = correlationId,
            TenantId = tenantId
        };
    }

    /// <summary>
    /// Deserializes the payload
    /// </summary>
    public T? GetPayload<T>()
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(Payload);
    }
}

/// <summary>
/// Outbox message status
/// </summary>
public enum OutboxMessageStatus
{
    /// <summary>
    /// Waiting to be processed
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Currently being processed
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Successfully processed
    /// </summary>
    Processed = 2,

    /// <summary>
    /// Failed to process
    /// </summary>
    Failed = 3
}

/// <summary>
/// Settings for outbox
/// </summary>
public class OutboxSettings
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// Whether outbox is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Processing interval in seconds
    /// </summary>
    public int ProcessingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Batch size for processing
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum retry count
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Retry delay in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 60;

    /// <summary>
    /// Cleanup interval in hours
    /// </summary>
    public int CleanupIntervalHours { get; set; } = 24;

    /// <summary>
    /// Retention period for processed messages in days
    /// </summary>
    public int RetentionDays { get; set; } = 7;

    /// <summary>
    /// Whether to use transaction
    /// </summary>
    public bool UseTransaction { get; set; } = true;
}
