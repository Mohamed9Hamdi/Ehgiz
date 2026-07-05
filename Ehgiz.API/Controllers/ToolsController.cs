using Ehgiz.Application.DTOs.Tools;
using Ehgiz.Application.Common;
using Ehgiz.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToolsController : ControllerBase
{
    private readonly IToolService _toolService;
    private readonly IToolSuggestionService _toolSuggestionService;
    private readonly IToolPhotoSearchService _toolPhotoSearchService;

    public ToolsController(
        IToolService toolService,
        IToolSuggestionService toolSuggestionService,
        IToolPhotoSearchService toolPhotoSearchService)
    {
        _toolService = toolService;
        _toolSuggestionService = toolSuggestionService;
        _toolPhotoSearchService = toolPhotoSearchService;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);





    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<ToolDto>>> GetAll([FromQuery] ToolFilterDto filter)
    {
        var result = await _toolService.GetAllAsync(filter);
        return Ok(result);
    }





    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ToolDto>> GetById(int id)
    {
        var tool = await _toolService.GetByIdAsync(id);
        return Ok(tool);
    }




    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<List<ToolDto>>> GetMyTools()
    {
        var tools = await _toolService.GetByOwnerAsync(CurrentUserId);
        return Ok(tools);
    }




    [HttpPost("suggest-from-images")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ToolSuggestionDto>> SuggestFromImages(
        [FromForm] List<IFormFile> images,
        CancellationToken cancellationToken)
    {
        if (images is null || images.Count == 0)
            return BadRequest(new { message = "At least one image is required." });

        var suggestion = await _toolSuggestionService.SuggestFromImagesAsync(images, cancellationToken);
        return Ok(suggestion);
    }

    [HttpPost("search-by-photo")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PhotoSearchResultDto>> SearchByPhoto(
        [FromForm] List<IFormFile> images,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (images is null || images.Count == 0)
            return BadRequest(new { message = "At least one image is required." });

        var result = await _toolPhotoSearchService.SearchByPhotoAsync(images, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ToolDto>> Create([FromBody] CreateToolDto dto)
    {
        var tool = await _toolService.CreateAsync(dto, CurrentUserId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = tool.Id },
            tool
        );
    }



    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ToolDto>> Update(int id, [FromBody] UpdateToolDto dto)
    {
        var tool = await _toolService.UpdateAsync(id, dto, CurrentUserId);
        return Ok(tool);
    }



    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        await _toolService.DeleteAsync(id, CurrentUserId);
        return NoContent();
    }




    [HttpPost("{id:int}/images")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<object>> UploadImages(
        int id,
        [FromForm] List<IFormFile> images)
    {
        var urls = await _toolService.UploadImagesAsync(id, images, CurrentUserId);

        return Ok(new
        {
            toolId = id,
            imageUrls = urls
        });
    }


    [HttpDelete("images/{imageId:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteImage(int imageId)
    {
        await _toolService.DeleteImageAsync(imageId, CurrentUserId);
        return NoContent();
    }


    [HttpPut("images/{imageId:int}/primary")]
    [Authorize]
    public async Task<IActionResult> SetPrimaryImage(int imageId)
    {
        await _toolService.SetPrimaryImageAsync(imageId, CurrentUserId);
        return NoContent();
    }
}