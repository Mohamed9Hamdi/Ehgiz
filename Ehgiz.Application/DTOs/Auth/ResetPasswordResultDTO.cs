namespace Ehgiz.Application.DTOs.Auth;

public record ResetPasswordResultDTO(
    bool Succeeded,
    string Message,
    IEnumerable<string> Errors);
