using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;

namespace Ehgiz.DAL.Repositories;

public class ConversationRepository : Repository<Conversation>, IConversationRepository
{
    public ConversationRepository(EhgizDbContext context) : base(context)
    {
    }
}
