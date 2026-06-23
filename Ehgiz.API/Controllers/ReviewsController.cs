using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Review;
using Ehgiz.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
        => _reviewService = reviewService;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET api/reviews/tool/{toolId}
    [HttpGet("tool/{toolId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByTool(int toolId)
    {
        var reviews = await _reviewService.GetByToolAsync(toolId);
        return Ok(ApiResponse<List<ReviewDto>>.Success(reviews));
    }

    // GET api/reviews/tool/{toolId}/rating
    [HttpGet("tool/{toolId:int}/rating")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRating(int toolId)
    {
        var avg = await _reviewService.GetAverageRatingAsync(toolId);
        return Ok(ApiResponse<object>.Success(new { toolId, averageRating = avg }));
    }

    // GET api/reviews/{id}
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var review = await _reviewService.GetByIdAsync(id);
        return Ok(ApiResponse<ReviewDto>.Success(review));
    }

    // POST api/reviews
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        var review = await _reviewService.CreateAsync(dto, CurrentUserId);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<ReviewDto>.Success(review, "Review submitted successfully."));
    }

    // DELETE api/reviews/{id}
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        await _reviewService.DeleteAsync(id, CurrentUserId);
        return NoContent();
    }
}
