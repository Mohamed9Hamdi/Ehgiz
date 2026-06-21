namespace Ehgiz.Application.DTOs.Auth;

public record VerifyEmailRequestDTO(
    string Email,
    string Code);
