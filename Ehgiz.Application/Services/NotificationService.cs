using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationBroadcaster _broadcaster;

    public NotificationService(IUnitOfWork uow, INotificationBroadcaster broadcaster)
    {
        _uow = uow;
        _broadcaster = broadcaster;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetByUserIdAsync(int userId)
    {
        return await _uow.Notifications.Query()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ProjectToType<NotificationDto>()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUnreadByUserIdAsync(int userId)
    {
        return await _uow.Notifications.Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ProjectToType<NotificationDto>()
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _uow.Notifications.GetUnreadCountAsync(userId);
    }

    public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
    {
        var notification = dto.Adapt<Notification>();
        notification.CreatedAt = DateTime.UtcNow;

        await _uow.Notifications.AddAsync(notification);
        await _uow.SaveChangesAsync();

        var result = notification.Adapt<NotificationDto>();

        await _broadcaster.SendToUserAsync(dto.UserId, result);

        return result;
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _uow.Notifications.GetByIdAsync(notificationId)
            ?? throw new KeyNotFoundException($"Notification {notificationId} not found");

        if (notification.UserId != userId)
            throw new UnauthorizedAccessException("Not your notification");

        await _uow.Notifications.MarkAsReadAsync(notificationId);
        await _broadcaster.NotifyReadStateChangedAsync(userId);
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        await _uow.Notifications.MarkAllAsReadAsync(userId);
        await _broadcaster.NotifyReadStateChangedAsync(userId);
    }

    public async Task DeleteAsync(int notificationId, int userId)
    {
        var notification = await _uow.Notifications.GetByIdAsync(notificationId)
            ?? throw new KeyNotFoundException($"Notification {notificationId} not found");

        if (notification.UserId != userId)
            throw new UnauthorizedAccessException("Not your notification");

        _uow.Notifications.Remove(notification);
        await _uow.SaveChangesAsync();
    }
}
