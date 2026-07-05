using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.DTOs.Auth;

public record ResetPasswordRequestDTO(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MaxLength(10)] string Code,
    [property: Required, MinLength(8), MaxLength(128)] string NewPassword);
