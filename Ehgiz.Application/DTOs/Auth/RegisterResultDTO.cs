namespace Ehgiz.Application.DTOs.Auth;

public record RegisterResultDTO(
    bool Succeeded,
    string? UserId,
    string? Message,
    IEnumerable<string> Errors);
