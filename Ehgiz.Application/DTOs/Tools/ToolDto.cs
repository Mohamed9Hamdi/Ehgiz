namespace Ehgiz.Application.DTOs.Tools;

public class ToolDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PricePerDay { get; set; }
    public decimal InsurancePrice { get; set; }
    public string? Condition { get; set; }
    public string? Location { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }

    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = null!;

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;

    public List<string> ImageUrls { get; set; } = [];
}
