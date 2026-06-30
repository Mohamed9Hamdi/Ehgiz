using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Ai;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IToolAssistantService _toolAssistantService;
    private readonly AiSettings _aiSettings;

    public AiController(IToolAssistantService toolAssistantService, IOptions<AiSettings> aiSettings)
    {
        _toolAssistantService = toolAssistantService;
        _aiSettings = aiSettings.Value;
    }

    [HttpPost("assistant")]
    public async Task<ActionResult<ApiResponse<ToolAssistantResponseDto>>> AskAssistant(
        [FromBody] ToolAssistantRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "Validation failed.",
                ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()));
        }

        if (string.IsNullOrWhiteSpace(dto.Question))
        {
            return BadRequest(ApiResponse<object>.Fail("Question is required."));
        }

        if (string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Fail("AI assistant is not configured. Please contact support."));
        }

        var result = await _toolAssistantService.RunAsync(dto.Question.Trim(), cancellationToken);

        return Ok(ApiResponse<ToolAssistantResponseDto>.Success(result));
    }
}
