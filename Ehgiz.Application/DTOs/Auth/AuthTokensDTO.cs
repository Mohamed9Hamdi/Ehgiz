namespace Ehgiz.Application.DTOs.Auth;

public record AuthTokensDTO(
    string AccessToken,
    string RawRefreshToken,
    DateTime AccessTokenExpiresAt,
    IList<string> Roles);
