namespace Ehgiz.Application.DTOs.Tools;

public class ToolFilterDto
{
    public int? CategoryId { get; set; }
    public string? Location { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? IsAvailable { get; set; }
    public string? SearchTerm { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
