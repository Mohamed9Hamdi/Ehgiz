namespace Ehgiz.Application.DTOs.Auth;

public record VerifyEmailResultDTO(
    bool Succeeded,
    string Message,
    IEnumerable<string> Errors);
