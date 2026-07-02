namespace Ehgiz.Application.DTOs.Auth;

public record ForgotPasswordResultDTO(
    bool Succeeded,
    string Message,
    IEnumerable<string> Errors);
