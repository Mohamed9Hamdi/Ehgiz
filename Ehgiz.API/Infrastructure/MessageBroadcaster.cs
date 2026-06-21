using Ehgiz.API.Hubs;
using Ehgiz.Application.DTOs.Messages;
using Ehgiz.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Ehgiz.API.Infrastructure;

public class MessageBroadcaster : IMessageBroadcaster
{
    private readonly IHubContext<ChatHub> _hub;

    public MessageBroadcaster(IHubContext<ChatHub> hub)
    {
        _hub = hub;
    }

    public async Task SendMessageAsync(int recipientId, MessageDto message)
    {
        await _hub.Clients
            .Group($"chat_user_{recipientId}")
            .SendAsync("ReceiveMessage", message);
    }

    public async Task NotifyReadAsync(int senderId, int conversationId)
    {
        await _hub.Clients
            .Group($"chat_user_{senderId}")
            .SendAsync("MessagesRead", new { conversationId });
    }
}
