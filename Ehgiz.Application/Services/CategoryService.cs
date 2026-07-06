using Ehgiz.Application.DTOs.Categories;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;

    public CategoryService(IUnitOfWork uow) => _uow = uow;

    public async Task<IEnumerable<CategoryDto>> GetActiveCategoriesAsync()
    {
        return await _uow.Categories.Query()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.ImageUrl,
                c.Tools.Count()))
            .ToListAsync();
    }
}
