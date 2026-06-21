namespace Ehgiz.Application.DTOs.Auth;

public record ResendResetCodeResultDTO(
    bool Succeeded,
    string Message,
    IEnumerable<string> Errors);
