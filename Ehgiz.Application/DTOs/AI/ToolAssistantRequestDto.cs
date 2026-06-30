using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.DTOs.Ai;

public class ToolAssistantRequestDto
{
    [Required]
    [MinLength(5)]
    [MaxLength(1000)]
    public string Question { get; set; } = string.Empty;
}
