using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<Conversation?> GetByUsersAsync(int user1Id, int user2Id);
    Task<IReadOnlyList<Conversation>> GetByUserIdWithDetailsAsync(int userId);
}
