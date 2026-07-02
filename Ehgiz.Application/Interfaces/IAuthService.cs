using Ehgiz.Application.DTOs.Auth;

namespace Ehgiz.Application.Interfaces;

public interface IAuthService
{
    Task<RegisterResultDTO> RegisterAsync(RegisterRequestDTO dto);
    Task<AuthLoginResultDTO> LoginAsync(LoginRequestDTO dto);
    Task<AuthTokensDTO?> RefreshSessionAsync(string rawRefreshToken);
    Task LogoutSessionAsync(string rawRefreshToken);
    Task<VerifyEmailResultDTO> VerifyEmailAsync(VerifyEmailRequestDTO dto);
    Task<ResendVerificationResultDTO> ResendVerificationAsync(ResendVerificationRequestDTO dto);
    Task<ForgotPasswordResultDTO> ForgotPasswordAsync(ForgotPasswordRequestDTO dto);
    Task<ResendResetCodeResultDTO> ResendResetCodeAsync(ResendResetCodeRequestDTO dto);
    Task<ResetPasswordResultDTO> ResetPasswordAsync(ResetPasswordRequestDTO dto);
}
