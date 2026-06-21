using Ehgiz.Application.DTOs.Messages;

namespace Ehgiz.Application.Interfaces;

public interface IMessageBroadcaster
{
    Task SendMessageAsync(int recipientId, MessageDto message);
    Task NotifyReadAsync(int senderId, int conversationId);
}
