using Ehgiz.DAL.Enums;

namespace Ehgiz.Application.DTOs.Tools;

public class UpdateToolDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PricePerDay { get; set; }
    public decimal InsurancePrice { get; set; }
    public ToolCondition? Condition { get; set; }
    public string? Location { get; set; }
    public bool IsAvailable { get; set; }
}
