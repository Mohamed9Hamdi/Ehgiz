using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;

namespace Ehgiz.DAL.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(EhgizDbContext context) : base(context)
    {
    }
}
