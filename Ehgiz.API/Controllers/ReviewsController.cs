using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Review;
using Ehgiz.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
        => _reviewService = reviewService;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("tool/{toolId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ReviewDto>>> GetByTool(int toolId)
    {
        var reviews = await _reviewService.GetByToolAsync(toolId);
        return Ok(reviews);
    }

    [HttpGet("tool/{toolId:int}/rating")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetRating(int toolId)
    {
        var avg = await _reviewService.GetAverageRatingAsync(toolId);
        return Ok(new { toolId, averageRating = avg });
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ReviewDto>> GetById(int id)
    {
        var review = await _reviewService.GetByIdAsync(id);
        return Ok(review);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReviewDto>> Create([FromBody] CreateReviewDto dto)
    {
        var review = await _reviewService.CreateAsync(dto, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = review.Id }, review);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        await _reviewService.DeleteAsync(id, CurrentUserId);
        return NoContent();
    }
}