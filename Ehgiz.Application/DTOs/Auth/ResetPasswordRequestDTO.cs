namespace Ehgiz.Application.DTOs.Auth;

public record ResetPasswordRequestDTO(
    string Email,
    string Code,
    string NewPassword);
