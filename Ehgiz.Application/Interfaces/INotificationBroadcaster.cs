using Ehgiz.Application.DTOs.Notifications;

namespace Ehgiz.Application.Interfaces;

public interface INotificationBroadcaster
{
    Task SendToUserAsync(int userId, NotificationDto notification);
}
