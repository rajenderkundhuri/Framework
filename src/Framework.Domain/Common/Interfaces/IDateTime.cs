namespace Framework.Domain.Common.Interfaces;

/// <summary>
/// Interface for datetime abstraction (enables testing)
/// </summary>
public interface IDateTime
{
    /// <summary>
    /// Gets the current UTC date and time
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Gets the current local date and time
    /// </summary>
    DateTimeOffset Now { get; }

    /// <summary>
    /// Gets today's date in UTC
    /// </summary>
    DateOnly Today { get; }
}

/// <summary>
/// Default implementation using system time
/// </summary>
public class SystemDateTime : IDateTime
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateTimeOffset Now => DateTimeOffset.Now;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
