namespace Framework.Application.Events;

/// <summary>
/// Configuration settings for event handling
/// </summary>
public class EventSettings
{
    public const string SectionName = "Events";

    /// <summary>
    /// Whether to throw exceptions from handlers or log them
    /// </summary>
    public bool ThrowOnHandlerException { get; set; } = false;

    /// <summary>
    /// Whether to dispatch events in parallel
    /// </summary>
    public bool ParallelDispatch { get; set; } = false;

    /// <summary>
    /// Maximum degree of parallelism for parallel dispatch
    /// </summary>
    public int MaxParallelism { get; set; } = 4;

    /// <summary>
    /// Whether to log all dispatched events
    /// </summary>
    public bool LogEvents { get; set; } = true;

    /// <summary>
    /// Event retention for replay (if supported)
    /// </summary>
    public TimeSpan EventRetention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Whether to use transactional outbox
    /// </summary>
    public bool UseOutbox { get; set; } = false;
}
