using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IToolRepository : IRepository<Tool>
{
    Task<(IReadOnlyList<Tool> Items, int TotalCount)> GetFilteredAsync(
        int? categoryId, string? location, decimal? minPrice, decimal? maxPrice,
        bool? isAvailable, string? searchTerm, int page, int pageSize);

    Task<(IReadOnlyList<Tool> Items, int TotalCount)> SearchByKeywordsAsync(
        IReadOnlyList<string> keywords,
        bool? isAvailable,
        int page,
        int pageSize);

    Task<Tool?> GetByIdWithDetailsAsync(int id);

    Task<IReadOnlyList<Tool>> GetByOwnerAsync(int ownerId);
}
