using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class MessageRepository : Repository<Message>, IMessageRepository
{
    private readonly EhgizDbContext _context;

    public MessageRepository(EhgizDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Message>> GetByConversationIdAsync(int conversationId, int page, int pageSize)
    {
        return await _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
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
