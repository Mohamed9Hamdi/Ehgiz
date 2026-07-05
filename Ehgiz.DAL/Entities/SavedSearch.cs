using Ehgiz.DAL.Enums;

namespace Ehgiz.DAL.Entities;

public class SavedSearch
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public string? Location { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public ToolCondition? Condition { get; set; }
    public DateTime CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Category? Category { get; set; }
}
