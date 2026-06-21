using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.DTOs.Messages;

public class SendMessageDto
{
    [Required, MinLength(1), MaxLength(2000)]
    public string Content { get; set; } = null!;
}
