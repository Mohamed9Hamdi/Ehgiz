using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;

namespace Ehgiz.DAL.Repositories;

public class ToolRepository : Repository<Tool>, IToolRepository
{
    public ToolRepository(EhgizDbContext context) : base(context)
    {
    }
}
