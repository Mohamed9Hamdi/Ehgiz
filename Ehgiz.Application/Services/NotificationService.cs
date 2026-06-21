using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces;
using Mapster;

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
        var notifications = await _uow.Notifications.GetByUserIdAsync(userId);
        return notifications.Adapt<List<NotificationDto>>();
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUnreadByUserIdAsync(int userId)
    {
        var notifications = await _uow.Notifications.GetUnreadByUserIdAsync(userId);
        return notifications.Adapt<List<NotificationDto>>();
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
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        await _uow.Notifications.MarkAllAsReadAsync(userId);
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
