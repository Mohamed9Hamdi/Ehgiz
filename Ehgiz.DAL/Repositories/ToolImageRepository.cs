using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class ToolImageRepository : Repository<ToolImage>, IToolImageRepository
{
    public ToolImageRepository(EhgizDbContext context) : base(context)
    {
    }

    public async Task<ToolImage?> GetByIdWithToolAsync(int id)
    {
        return await _context.ToolImages
            .Include(i => i.Tool)
            .FirstOrDefaultAsync(i => i.Id == id);
    }
}
