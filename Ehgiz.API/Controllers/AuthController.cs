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
            return BadRequest(new { errors = result.Errors });

        return StatusCode(StatusCodes.Status201Created, new { message = result.Message });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO dto)
    {
        var tokens = await _authService.LoginAsync(dto);
        if (tokens is null)
            return Unauthorized();

        SetRefreshCookie(tokens.RawRefreshToken);

        return Ok(new LoginResponseDTO(tokens.AccessToken, tokens.AccessTokenExpiresAt));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var rawRefreshToken) ||
            string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Unauthorized();
        }

        var tokens = await _authService.RefreshSessionAsync(rawRefreshToken);
        if (tokens is null)
        {
            DeleteRefreshCookie();
            return Unauthorized();
        }

        SetRefreshCookie(tokens.RawRefreshToken);

        return Ok(new LoginResponseDTO(tokens.AccessToken, tokens.AccessTokenExpiresAt));
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
