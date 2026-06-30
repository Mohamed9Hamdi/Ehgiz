using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class MessageRepository : Repository<Message>, IMessageRepository
{
    public MessageRepository(EhgizDbContext context) : base(context)
    {
    }

    public async Task MarkConversationAsReadAsync(int conversationId, int readerUserId)
    {
        var now = DateTime.UtcNow;
        await _context.Messages
            .Where(m => m.ConversationId == conversationId
                     && m.SenderId != readerUserId
                     && m.Status != MessageStatus.Read)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, MessageStatus.Read)
                .SetProperty(m => m.ReadAt, now));
    }

    public async Task<Dictionary<int, int>> GetUnreadCountsByUserAsync(int userId, IEnumerable<int> conversationIds)
    {
        return await _context.Messages
            .Where(m => conversationIds.Contains(m.ConversationId)
                     && m.SenderId != userId
                     && m.Status != MessageStatus.Read)
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConversationId, x => x.Count);
    }
}
