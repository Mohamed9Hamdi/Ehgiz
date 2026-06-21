using Ehgiz.Application.DTOs.Messages;

namespace Ehgiz.Application.Interfaces;

public interface IMessageService
{
    Task<ConversationDto> GetOrCreateConversationAsync(int currentUserId, int otherUserId);
    Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(int userId);
    Task<IReadOnlyList<MessageDto>> GetMessagesAsync(int conversationId, int userId, int page, int pageSize);
    Task<MessageDto> SendMessageAsync(int conversationId, int senderId, SendMessageDto dto);
    Task MarkAsReadAsync(int conversationId, int userId);
}
