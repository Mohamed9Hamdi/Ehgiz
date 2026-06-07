namespace Ehgiz.DAL.Entities;

public class Tool
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PricePerDay { get; set; }
    public decimal InsurancePrice { get; set; }
    public string? Condition { get; set; }
    public string? Location { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Owner { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<ToolImage> Images { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
}
