namespace Ehgiz.Application.DTOs.Ai;

public class ToolRecommendationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PricePerDay { get; set; }
    public string CategoryName { get; set; } = null!;
    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<string> ImageUrls { get; set; } = [];
}
