using Framework.Application.Common.Models;
using Framework.Domain.Notifications;

namespace Framework.Application.Notifications;

/// <summary>
/// Service for managing in-app notifications
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Gets notifications for a user with pagination
    /// </summary>
    Task<PagedList<NotificationResponse>> GetUserNotificationsAsync(
        Guid userId,
        NotificationListRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unread notification count for a user
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific notification
    /// </summary>
    Task<NotificationResponse?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new notification
    /// </summary>
    Task<Result<Guid>> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates notifications for multiple users
    /// </summary>
    Task<Result<int>> CreateBulkAsync(CreateBulkNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    Task<Result> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all notifications as read for a user
    /// </summary>
    Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a notification as unread
    /// </summary>
    Task<Result> MarkAsUnreadAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a notification
    /// </summary>
    Task<Result> DeleteAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all read notifications for a user
    /// </summary>
    Task<Result<int>> DeleteAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}

#region Request DTOs

/// <summary>
/// Notification list request
/// </summary>
public class NotificationListRequest
{
    public bool? IsRead { get; set; }
    public NotificationType? Type { get; set; }
    public NotificationSeverity? Severity { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Create notification request
/// </summary>
public class CreateNotificationRequest
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
    public string? ActionUrl { get; set; }
    public string? Metadata { get; set; }
}

/// <summary>
/// Create bulk notification request
/// </summary>
public class CreateBulkNotificationRequest
{
    public List<Guid> UserIds { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
    public string? ActionUrl { get; set; }
    public string? Metadata { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// Notification response
/// </summary>
public class NotificationResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationSeverity Severity { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public string? ActionUrl { get; set; }
    public string? Metadata { get; set; }
}

#endregion
