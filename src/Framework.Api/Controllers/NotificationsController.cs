using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Framework.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Controllers;

/// <summary>
/// Notification management endpoints
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Get notifications for current user
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedList<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications([FromQuery] NotificationListRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _notificationService.GetUserNotificationsAsync(userId.Value, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get unread notification count for current user
    /// </summary>
    [HttpGet("my/unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyUnreadCount(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var count = await _notificationService.GetUnreadCountAsync(userId.Value, cancellationToken);
        return Ok(count);
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    [HttpPost("{id:guid}/mark-read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var result = await _notificationService.MarkAsReadAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Mark a notification as unread
    /// </summary>
    [HttpPost("{id:guid}/mark-unread")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsUnread(Guid id, CancellationToken cancellationToken)
    {
        var result = await _notificationService.MarkAsUnreadAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Mark all notifications as read for current user
    /// </summary>
    [HttpPost("my/mark-all-read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _notificationService.MarkAllAsReadAsync(userId.Value, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken cancellationToken)
    {
        var result = await _notificationService.DeleteAsync(id, cancellationToken);
        return HandleDeleteResult(result);
    }

    /// <summary>
    /// Delete all read notifications for current user
    /// </summary>
    [HttpDelete("my/read")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAllReadNotifications(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _notificationService.DeleteAllReadAsync(userId.Value, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get all notifications (admin)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllNotifications([FromQuery] NotificationListRequest request, [FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.NotificationsView))
            return Forbid();

        if (userId.HasValue)
        {
            var result = await _notificationService.GetUserNotificationsAsync(userId.Value, request, cancellationToken);
            return Ok(result);
        }

        // If no userId, return all notifications (admin view)
        var allNotifications = await _notificationService.GetUserNotificationsAsync(Guid.Empty, request, cancellationToken);
        return Ok(allNotifications);
    }

    /// <summary>
    /// Create a notification for a user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.NotificationsCreate))
            return Forbid();

        var result = await _notificationService.CreateAsync(request, cancellationToken);
        return HandleCreatedResult(result, nameof(GetNotification), new { id = result.Value });
    }

    /// <summary>
    /// Get notification by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotification(Guid id, CancellationToken cancellationToken)
    {
        var notification = await _notificationService.GetByIdAsync(id, cancellationToken);
        if (notification == null)
            return NotFound();

        return Ok(notification);
    }

    /// <summary>
    /// Create bulk notifications
    /// </summary>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBulkNotifications([FromBody] CreateBulkNotificationRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.NotificationsCreate))
            return Forbid();

        var result = await _notificationService.CreateBulkAsync(request, cancellationToken);
        return HandleResult(result);
    }
}
