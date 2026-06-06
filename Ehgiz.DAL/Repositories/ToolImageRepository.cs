using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;

namespace Ehgiz.DAL.Repositories;

public class ToolImageRepository : Repository<ToolImage>, IToolImageRepository
{
    public ToolImageRepository(EhgizDbContext context) : base(context)
    {
    }
}
