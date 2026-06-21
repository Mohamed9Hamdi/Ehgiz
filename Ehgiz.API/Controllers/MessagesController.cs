using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Messages;
using Ehgiz.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
[Produces("application/json")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessagesController(IMessageService messageService)
        => _messageService = messageService;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("conversations")]
    [ProducesResponseType(typeof(ApiResponse<ConversationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrCreateConversation([FromBody] StartConversationDto dto)
    {
        var conversation = await _messageService.GetOrCreateConversationAsync(CurrentUserId, dto.RecipientId);
        return Ok(ApiResponse<ConversationDto>.Success(conversation));
    }

    [HttpGet("conversations")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ConversationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations()
    {
        var conversations = await _messageService.GetConversationsAsync(CurrentUserId);
        return Ok(ApiResponse<IReadOnlyList<ConversationDto>>.Success(conversations));
    }

    [HttpGet("conversations/{conversationId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MessageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(
        int conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        var messages = await _messageService.GetMessagesAsync(conversationId, CurrentUserId, page, pageSize);
        return Ok(ApiResponse<IReadOnlyList<MessageDto>>.Success(messages));
    }

    [HttpPost("conversations/{conversationId:int}")]
    [ProducesResponseType(typeof(ApiResponse<MessageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(int conversationId, [FromBody] SendMessageDto dto)
    {
        var message = await _messageService.SendMessageAsync(conversationId, CurrentUserId, dto);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<MessageDto>.Success(message));
    }

    [HttpPut("conversations/{conversationId:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int conversationId)
    {
        await _messageService.MarkAsReadAsync(conversationId, CurrentUserId);
        return NoContent();
    }
}
