using Ehgiz.DAL.Enums;
using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.DTOs.Tools;

public class UpdateToolDto
{
    public int CategoryId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(5000)]
    public string? Description { get; set; }

    [Range(0.01, 1_000_000)]
    public decimal PricePerDay { get; set; }

    [Range(0, 1_000_000)]
    public decimal InsurancePrice { get; set; }

    public ToolCondition? Condition { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    public bool IsAvailable { get; set; }
}
