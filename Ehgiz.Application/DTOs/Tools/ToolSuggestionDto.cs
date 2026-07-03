using Ehgiz.DAL.Enums;

namespace Ehgiz.Application.DTOs.Tools;

public class ToolSuggestionDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public ToolCondition Condition { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
}
