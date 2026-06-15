using Ehgiz.Application.DTOs.Auth;

namespace Ehgiz.Application.Interfaces;

public interface IAuthService
{
    Task<RegisterResultDTO> RegisterAsync(RegisterRequestDTO dto);
    Task<AuthTokensDTO?> LoginAsync(LoginRequestDTO dto);
    Task<AuthTokensDTO?> RefreshSessionAsync(string rawRefreshToken);
    Task LogoutSessionAsync(string rawRefreshToken);
}
