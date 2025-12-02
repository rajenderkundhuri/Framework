using Framework.Domain.Common.Entities;

namespace Framework.Domain.Notifications;

/// <summary>
/// In-app notification entity
/// </summary>
public class Notification : Entity<Guid>
{
    private Notification() : base() { }

    public Notification(
        Guid id,
        Guid userId,
        string title,
        string message,
        NotificationType type,
        NotificationSeverity severity = NotificationSeverity.Info)
        : base(id)
    {
        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        Severity = severity;
        CreatedAt = DateTimeOffset.UtcNow;
        IsRead = false;
    }

    /// <summary>
    /// User ID who receives the notification
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Notification title
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Notification message/body
    /// </summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Type of notification
    /// </summary>
    public NotificationType Type { get; private set; }

    /// <summary>
    /// Severity level
    /// </summary>
    public NotificationSeverity Severity { get; private set; }

    /// <summary>
    /// Whether the notification has been read
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// When the notification was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// When the notification was read
    /// </summary>
    public DateTimeOffset? ReadAt { get; private set; }

    /// <summary>
    /// Optional link/URL for action
    /// </summary>
    public string? ActionUrl { get; private set; }

    /// <summary>
    /// Optional metadata (JSON)
    /// </summary>
    public string? Metadata { get; private set; }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Mark notification as unread
    /// </summary>
    public void MarkAsUnread()
    {
        IsRead = false;
        ReadAt = null;
    }

    /// <summary>
    /// Set action URL
    /// </summary>
    public void SetActionUrl(string? url) => ActionUrl = url;

    /// <summary>
    /// Set metadata
    /// </summary>
    public void SetMetadata(string? metadata) => Metadata = metadata;
}

/// <summary>
/// Types of notifications
/// </summary>
public enum NotificationType
{
    System = 0,
    Security = 1,
    UserAction = 2,
    Reminder = 3,
    Alert = 4,
    Info = 5
}

/// <summary>
/// Notification severity levels
/// </summary>
public enum NotificationSeverity
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3
}
