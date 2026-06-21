using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IMessageRepository : IRepository<Message>
{
    Task<IReadOnlyList<Message>> GetByConversationIdAsync(int conversationId, int page, int pageSize);
    Task MarkConversationAsReadAsync(int conversationId, int readerUserId);
    Task<Dictionary<int, int>> GetUnreadCountsByUserAsync(int userId, IEnumerable<int> conversationIds);
}
