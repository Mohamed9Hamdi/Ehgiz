using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;

namespace Ehgiz.DAL.Repositories;

public class MessageRepository : Repository<Message>, IMessageRepository
{
    public MessageRepository(EhgizDbContext context) : base(context)
    {
    }
}
