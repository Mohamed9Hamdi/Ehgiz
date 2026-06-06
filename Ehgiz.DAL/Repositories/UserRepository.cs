using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;

namespace Ehgiz.DAL.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(EhgizDbContext context) : base(context)
    {
    }
}
