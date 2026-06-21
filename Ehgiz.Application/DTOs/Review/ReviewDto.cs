namespace Ehgiz.Application.DTOs.Review;

public class ReviewDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public int ToolId { get; set; }
    public string ToolName { get; set; } = null!;
    public string RenterName { get; set; } = null!;
}
