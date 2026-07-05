using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.DTOs.Auth;

public record ResetPasswordRequestDTO(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(10)] string Code,
    [Required, MinLength(8), MaxLength(128)] string NewPassword
);
