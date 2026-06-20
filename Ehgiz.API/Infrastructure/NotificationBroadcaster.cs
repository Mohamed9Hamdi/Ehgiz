using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.Interfaces;
using Ehgiz.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Ehgiz.API.Infrastructure;

public class NotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationBroadcaster(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public async Task SendToUserAsync(int userId, NotificationDto notification)
    {
        await _hub.Clients
            .Group($"user_{userId}")
            .SendAsync("ReceiveNotification", notification);
    }
}
