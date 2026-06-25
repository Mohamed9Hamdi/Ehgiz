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
        return await _context.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .ThenInclude(m => m.Sender)
            .OrderByDescending(c => c.UpdatedAt)
            .AsNoTracking()
            .ToListAsync();
    }
}
