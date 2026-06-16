namespace Ehgiz.Application.DTOs.Auth;

public record AuthLoginResultDTO(
    AuthTokensDTO? Tokens,
    string? FailureMessage);
