using Ehgiz.DAL.Enums;
using System.ComponentModel.DataAnnotations;

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

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    public bool IsAvailable { get; set; }
}
