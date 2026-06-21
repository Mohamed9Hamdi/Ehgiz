using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IToolImageRepository : IRepository<ToolImage>
{
    Task<ToolImage?> GetByIdWithToolAsync(int id);
}
