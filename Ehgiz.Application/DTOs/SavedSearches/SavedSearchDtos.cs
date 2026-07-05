using Ehgiz.DAL.Enums;

namespace Ehgiz.Application.DTOs.SavedSearches;

public class CreateSavedSearchDto
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public string? Location { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public ToolCondition? Condition { get; set; }
}

public class SavedSearchDto
{
    public int Id { get; set; }
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Location { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Condition { get; set; }
    public DateTime CreatedAt { get; set; }
}
