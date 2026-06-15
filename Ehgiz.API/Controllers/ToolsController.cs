using Ehgiz.Application.DTOs.Tools;
using Ehgiz.BLL.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToolsController : ControllerBase
{
    private readonly IToolService _toolService;

    public ToolsController(IToolService toolService)
    {
        _toolService = toolService;
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
}