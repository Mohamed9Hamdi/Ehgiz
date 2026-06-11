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

    // من الـ Owner (ApplicationUser)
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = null!;

    // من الـ Category
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;

    // صور الأداة
    public List<string> ImageUrls { get; set; } = [];
}