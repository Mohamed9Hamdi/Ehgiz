namespace Ehgiz.Application.DTOs.Auth;

public record LoginResponseDTO(
    string AccessToken,
    DateTime ExpiresAt);
