namespace Ehgiz.Application.DTOs.Ai;

public class ToolAssistantResponseDto
{
    public string Answer { get; set; } = string.Empty;
    public List<ToolRecommendationDto> RecommendedTools { get; set; } = [];
}
