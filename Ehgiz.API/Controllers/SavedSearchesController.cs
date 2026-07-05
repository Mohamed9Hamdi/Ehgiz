using System.Security.Claims;
using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.SavedSearches;
using Ehgiz.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/saved-searches")]
[Authorize]
public class SavedSearchesController : ControllerBase
{
    private readonly ISavedSearchService _savedSearchService;

    public SavedSearchesController(ISavedSearchService savedSearchService)
    {
        _savedSearchService = savedSearchService;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // POST api/saved-searches
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSavedSearchDto dto)
    {
        var result = await _savedSearchService.CreateAsync(CurrentUserId, dto);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<SavedSearchDto>.Success(result, "Saved search created."));
    }

    // GET api/saved-searches
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _savedSearchService.GetAllForUserAsync(CurrentUserId);
        return Ok(ApiResponse<List<SavedSearchDto>>.Success(result));
    }

    // DELETE api/saved-searches/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _savedSearchService.DeleteAsync(id, CurrentUserId);
        return Ok(ApiResponse<object>.Success(null!, "Saved search deleted."));
    }
}
