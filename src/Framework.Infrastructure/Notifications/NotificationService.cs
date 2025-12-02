using Framework.Application.Common.Models;
using Framework.Application.Notifications;
using Framework.Domain.Notifications;
using Framework.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Framework.Infrastructure.Notifications;

/// <summary>
/// Implementation of the notification service
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<NotificationResponse>> GetUserNotificationsAsync(
        Guid userId,
        NotificationListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        // Apply filters
        if (request.IsRead.HasValue)
        {
            query = query.Where(n => n.IsRead == request.IsRead.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(n => n.Type == request.Type.Value);
        }

        if (request.Severity.HasValue)
        {
            query = query.Where(n => n.Severity == request.Severity.Value);
        }

        // Apply sorting
        query = request.SortDescending
            ? query.OrderByDescending(n => n.CreatedAt)
            : query.OrderBy(n => n.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => MapToResponse(n))
            .ToListAsync(cancellationToken);

        return new PagedList<NotificationResponse>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync(cancellationToken);
    }

    public async Task<NotificationResponse?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        return notification != null ? MapToResponse(notification) : null;
    }

    public async Task<Result<Guid>> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var notification = new Notification(
            Guid.NewGuid(),
            request.UserId,
            request.Title,
            request.Message,
            request.Type,
            request.Severity);

        if (!string.IsNullOrEmpty(request.ActionUrl))
        {
            notification.SetActionUrl(request.ActionUrl);
        }

        if (!string.IsNullOrEmpty(request.Metadata))
        {
            notification.SetMetadata(request.Metadata);
        }

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(notification.Id);
    }

    public async Task<Result<int>> CreateBulkAsync(CreateBulkNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var notifications = request.UserIds.Select(userId =>
        {
            var notification = new Notification(
                Guid.NewGuid(),
                userId,
                request.Title,
                request.Message,
                request.Type,
                request.Severity);

            if (!string.IsNullOrEmpty(request.ActionUrl))
            {
                notification.SetActionUrl(request.ActionUrl);
            }

            if (!string.IsNullOrEmpty(request.Metadata))
            {
                notification.SetMetadata(request.Metadata);
            }

            return notification;
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(notifications.Count);
    }

    public async Task<Result> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification == null)
        {
            return Result.Failure("Notification not found");
        }

        notification.MarkAsRead();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> MarkAsUnreadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification == null)
        {
            return Result.Failure("Notification not found");
        }

        notification.MarkAsUnread();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification == null)
        {
            return Result.Failure("Notification not found");
        }

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<int>> DeleteAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && n.IsRead)
            .ToListAsync(cancellationToken);

        var count = notifications.Count;
        _context.Notifications.RemoveRange(notifications);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(count);
    }

    private static NotificationResponse MapToResponse(Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            Severity = notification.Severity,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt,
            ActionUrl = notification.ActionUrl,
            Metadata = notification.Metadata
        };
    }
}
