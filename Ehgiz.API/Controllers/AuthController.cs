using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Auth;
using Ehgiz.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "X-Refresh-Token";
    private const string RefreshCookiePath = "/api/auth/refresh";

    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO dto)
    {
        var result = await _authService.RegisterAsync(dto);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "Registration failed.",
                result.Errors.ToList()));
        }

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<object>.Success(null!, result.Message!));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO dto)
    {
        var tokens = await _authService.LoginAsync(dto);
        if (tokens is null)
        {
            return Unauthorized(ApiResponse<LoginResponseDTO>.Fail("Invalid email or password."));
        }

        SetRefreshCookie(tokens.RawRefreshToken);

        return Ok(ApiResponse<LoginResponseDTO>.Success(
            new LoginResponseDTO(tokens.AccessToken, tokens.AccessTokenExpiresAt),
            "Login successful"));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var rawRefreshToken) ||
            string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Unauthorized(ApiResponse<LoginResponseDTO>.Fail("Invalid or expired refresh token."));
        }

        var tokens = await _authService.RefreshSessionAsync(rawRefreshToken);
        if (tokens is null)
        {
            DeleteRefreshCookie();
            return Unauthorized(ApiResponse<LoginResponseDTO>.Fail("Invalid or expired refresh token."));
        }

        SetRefreshCookie(tokens.RawRefreshToken);

        return Ok(ApiResponse<LoginResponseDTO>.Success(
            new LoginResponseDTO(tokens.AccessToken, tokens.AccessTokenExpiresAt),
            "Token refreshed successfully"));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var rawRefreshToken) &&
            !string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            await _authService.LogoutAsync(rawRefreshToken);
        }

        DeleteRefreshCookie();
        return NoContent();
    }

    private void SetRefreshCookie(string rawRefreshToken)
    {
        Response.Cookies.Append(RefreshCookieName, rawRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath
        });
    }

    private void DeleteRefreshCookie()
    {
        Response.Cookies.Append(RefreshCookieName, string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = DateTimeOffset.UnixEpoch,
            MaxAge = TimeSpan.Zero
        });
    }
}
