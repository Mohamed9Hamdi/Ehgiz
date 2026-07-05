using System.Security.Claims;
using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Auth;
using Ehgiz.Application.DTOs.Profile;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Services;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "X-Refresh-Token";
    private const string RefreshCookiePath = "/api/auth/refresh";

    private readonly IAuthService _authService;
    private readonly IProfileService _profileService;
    private readonly IMapper _mapper;

    public AuthController(
        IAuthService authService,
        IProfileService profileService,
        IMapper mapper)
    {
        _authService = authService;
        _profileService = profileService;
        _mapper = mapper;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromForm] RegisterRequestDTO dto)
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
        var result = await _authService.LoginAsync(dto);
        if (result.Tokens is null)
        {
            return Unauthorized(ApiResponse<LoginResponseDTO>.Fail(
                result.FailureMessage ?? "Invalid email or password."));
        }

        SetRefreshCookie(result.Tokens.RawRefreshToken);

        return Ok(ApiResponse<LoginResponseDTO>.Success(
            _mapper.Map<LoginResponseDTO>(result.Tokens),
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
            _mapper.Map<LoginResponseDTO>(tokens),
            "Token refreshed successfully"));
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDTO dto)
    {
        var result = await _authService.VerifyEmailAsync(dto);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<object>.Fail(
                result.Message,
                result.Errors.ToList()));
        }

        return Ok(ApiResponse<object>.Success(null!, result.Message));
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequestDTO dto)
    {
        var result = await _authService.ResendVerificationAsync(dto);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message));
        }

        return Ok(ApiResponse<object>.Success(null!, result.Message));
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDTO dto)
    {
        var result = await _authService.ForgotPasswordAsync(dto);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<object>.Fail(
                result.Message,
                result.Errors.ToList()));
        }

        return Ok(ApiResponse<object>.Success(null!, result.Message));
    }

    [HttpPost("resend-reset-code")]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ResendResetCode([FromBody] ResendResetCodeRequestDTO dto)
    {
        var result = await _authService.ResendResetCodeAsync(dto);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<object>.Fail(
                result.Message,
                result.Errors.ToList()));
        }

        return Ok(ApiResponse<object>.Success(null!, result.Message));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO dto)
    {
        var result = await _authService.ResetPasswordAsync(dto);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<object>.Fail(
                result.Message,
                result.Errors.ToList()));
        }

        return Ok(ApiResponse<object>.Success(null!, result.Message));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<UserProfileDTO>.Fail("Unauthorized."));
        }

        var profile = await _profileService.GetProfileAsync(userId);
        if (profile is null)
        {
            return NotFound(ApiResponse<UserProfileDTO>.Fail("User not found."));
        }

        return Ok(ApiResponse<UserProfileDTO>.Success(profile, "Profile retrieved successfully."));
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<UserProfileDTO>.Fail("Unauthorized."));
        }

        var profile = await _profileService.UpdateProfileAsync(userId, dto);
        if (profile is null)
        {
            return NotFound(ApiResponse<UserProfileDTO>.Fail("User not found."));
        }

        return Ok(ApiResponse<UserProfileDTO>.Success(profile, "Profile updated successfully."));
    }

    [Authorize]
    [HttpPost("me/profile-image")]
    public async Task<IActionResult> UpdateProfileImage(IFormFile image)
    {
        if (image is null || image.Length == 0)
        {
            return BadRequest(ApiResponse<UserProfileDTO>.Fail("Image file is required."));
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<UserProfileDTO>.Fail("Unauthorized."));
        }

        var profile = await _profileService.UpdateProfileImageAsync(userId, image);
        if (profile is null)
        {
            return NotFound(ApiResponse<UserProfileDTO>.Fail("User not found."));
        }

        return Ok(ApiResponse<UserProfileDTO>.Success(profile, "Profile image updated successfully."));
    }

    [Authorize]
    [HttpDelete("me/profile-image")]
    public async Task<IActionResult> RemoveProfileImage()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<UserProfileDTO>.Fail("Unauthorized."));
        }

        var profile = await _profileService.RemoveProfileImageAsync(userId);
        if (profile is null)
        {
            return NotFound(ApiResponse<UserProfileDTO>.Fail("User not found."));
        }

        return Ok(ApiResponse<UserProfileDTO>.Success(profile, "Profile image removed successfully."));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var rawRefreshToken) &&
            !string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            await _authService.LogoutSessionAsync(rawRefreshToken);
        }

        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath
        });

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
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath
        });
    }
}
