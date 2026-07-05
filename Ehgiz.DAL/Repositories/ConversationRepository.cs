using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class ConversationRepository : Repository<Conversation>, IConversationRepository
{
    public ConversationRepository(EhgizDbContext context) : base(context)
    {
    }

    public async Task<Conversation?> GetByUsersAsync(int user1Id, int user2Id)
    {
        return await _context.Conversations
            .FirstOrDefaultAsync(c =>
                (c.User1Id == user1Id && c.User2Id == user2Id) ||
                (c.User1Id == user2Id && c.User2Id == user1Id));
    }

    public async Task<IReadOnlyList<Conversation>> GetByUserIdWithDetailsAsync(int userId)
    {
        var conversations = await _context.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Include(c => c.User1)
            .Include(c => c.User2)
            .OrderByDescending(c => c.UpdatedAt)
            .AsNoTracking()
            .ToListAsync();

        if (conversations.Count == 0)
            return conversations;

        var ids = conversations.Select(c => c.Id).ToList();

        // Latest message per conversation. Max(Id) is used instead of a
        // filtered Include(...Take(1)) because that needs the APPLY operator,
        // which SQLite doesn't support; ids are identity-ordered so the max id
        // is the newest message.
        var lastMessageIds = await _context.Messages
            .Where(m => ids.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.Max(m => m.Id))
            .ToListAsync();

        var lastMessages = await _context.Messages
            .Include(m => m.Sender)
            .Where(m => lastMessageIds.Contains(m.Id))
            .AsNoTracking()
            .ToListAsync();

        foreach (var conversation in conversations)
        {
            conversation.Messages = lastMessages
                .Where(m => m.ConversationId == conversation.Id)
                .ToList();
        }

        return conversations;
    }
}
