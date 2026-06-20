using Ehgiz.Application.DTOs.Notifications;

namespace Ehgiz.Application.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetByUserIdAsync(int userId);
    Task<IReadOnlyList<NotificationDto>> GetUnreadByUserIdAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<NotificationDto> CreateAsync(CreateNotificationDto dto);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task DeleteAsync(int notificationId, int userId);
}
