using Ehgiz.Application.DTOs.Categories;

namespace Ehgiz.Application.Interfaces;

public interface ICategoryService
{
    /// <summary>Active categories, ordered by name, for public dropdowns and filters.</summary>
    Task<IEnumerable<CategoryDto>> GetActiveCategoriesAsync();
}
