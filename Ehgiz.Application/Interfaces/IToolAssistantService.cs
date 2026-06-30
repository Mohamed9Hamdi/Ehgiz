using Ehgiz.Application.DTOs.Ai;

namespace Ehgiz.Application.Interfaces;

public interface IToolAssistantService
{
    Task<ToolAssistantResponseDto> RunAsync(string userMessage, CancellationToken cancellationToken = default);
}
