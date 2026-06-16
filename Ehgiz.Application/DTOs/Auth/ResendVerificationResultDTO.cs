namespace Ehgiz.Application.DTOs.Auth;

public record ResendVerificationResultDTO(
    bool Succeeded,
    string Message);
