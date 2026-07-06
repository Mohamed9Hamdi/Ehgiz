using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Categories;
using Ehgiz.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    // GET api/categories — public so browse filters and the add/edit-tool forms
    // can show the current, admin-managed category list.
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _categoryService.GetActiveCategoriesAsync();
        return Ok(ApiResponse<IEnumerable<CategoryDto>>.Success(result));
    }
}
