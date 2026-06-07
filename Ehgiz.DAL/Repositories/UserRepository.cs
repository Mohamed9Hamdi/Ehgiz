using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;

namespace Ehgiz.DAL.Repositories;

public class UserRepository : Repository<ApplicationUser>, IUserRepository
{
    public UserRepository(EhgizDbContext context) : base(context)
    {
    }
}
