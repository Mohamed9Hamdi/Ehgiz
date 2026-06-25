using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class ToolRepository : Repository<Tool>, IToolRepository
{
    public ToolRepository(EhgizDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<Tool> Items, int TotalCount)> GetFilteredAsync(
        int? categoryId, string? location, decimal? minPrice, decimal? maxPrice,
        bool? isAvailable, string? searchTerm, int page, int pageSize)
    {
        var query = _context.Tools
            .Include(t => t.Owner)
            .Include(t => t.Category)
            .Include(t => t.Images)
            .AsNoTracking()
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(t => t.Location!.Contains(location));

        if (minPrice.HasValue)
            query = query.Where(t => t.PricePerDay >= minPrice);

        if (maxPrice.HasValue)
            query = query.Where(t => t.PricePerDay <= maxPrice);

        if (isAvailable.HasValue)
            query = query.Where(t => t.IsAvailable == isAvailable);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(t =>
                t.Name.Contains(searchTerm) ||
                (t.Description != null && t.Description.Contains(searchTerm)));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Tool?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Tools
            .Include(t => t.Owner)
            .Include(t => t.Category)
            .Include(t => t.Images)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IReadOnlyList<Tool>> GetByOwnerAsync(int ownerId)
    {
        return await _context.Tools
            .Include(t => t.Owner)
            .Include(t => t.Category)
            .Include(t => t.Images)
            .AsNoTracking()
            .Where(t => t.OwnerId == ownerId)
            .ToListAsync();
    }
}
