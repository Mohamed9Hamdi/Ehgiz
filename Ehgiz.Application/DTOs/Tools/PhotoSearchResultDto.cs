using Ehgiz.Application.Common;

namespace Ehgiz.Application.DTOs.Tools;

public class PhotoSearchResultDto
{
    public string IdentifiedObject { get; set; } = null!;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public IReadOnlyList<string> SearchKeywords { get; set; } = [];
    public PagedResult<ToolDto> MatchingTools { get; set; } = null!;
}
