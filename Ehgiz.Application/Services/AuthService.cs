using System.Security.Cryptography;
using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Auth;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Settings;
using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ehgiz.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly EhgizDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly JwtSettings _jwtSettings;
    private readonly SendGridSettings _sendGridSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        EhgizDbContext context,
        ITokenService tokenService,
        IEmailService emailService,
        IOptions<JwtSettings> jwtSettings,
        IOptions<SendGridSettings> sendGridSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _tokenService = tokenService;
        _emailService = emailService;
        _jwtSettings = jwtSettings.Value;
        _sendGridSettings = sendGridSettings.Value;
    }

    public async Task<RegisterResultDTO> RegisterAsync(RegisterRequestDTO dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser is not null)
        {
            return new RegisterResultDTO(
                false,
                null,
                null,
                ["Email is already registered."]);
        }

        var user = dto.Adapt<ApplicationUser>();

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return new RegisterResultDTO(
                false,
                null,
                null,
                result.Errors.Select(e => e.Description));
        }

        await _userManager.AddToRoleAsync(user, AppRoles.User);

        await CreateAndSendVerificationCodeAsync(user);

        return new RegisterResultDTO(
            true,
            user.Id.ToString(),
            "Account created. Check your email for the verification code.",
            []);
    }

    public async Task<AuthLoginResultDTO> LoginAsync(LoginRequestDTO dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return new AuthLoginResultDTO(null, "Invalid email or password.");

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (signInResult.Succeeded)
            return new AuthLoginResultDTO(await IssueTokensAsync(user), null);

        if (signInResult.IsNotAllowed)
            return new AuthLoginResultDTO(null, "Please verify your email before logging in.");

        return new AuthLoginResultDTO(null, "Invalid email or password.");
    }

    public async Task<VerifyEmailResultDTO> VerifyEmailAsync(VerifyEmailRequestDTO dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            return new VerifyEmailResultDTO(
                false,
                "Invalid or expired verification code.",
                []);
        }

        if (user.EmailConfirmed)
        {
            return new VerifyEmailResultDTO(
                true,
                "Email is already verified.",
                []);
        }

        var hash = _tokenService.HashToken(dto.Code.Trim());
        var stored = await _context.EmailVerificationCodes
            .FirstOrDefaultAsync(c => c.UserId == user.Id && c.CodeHash == hash);

        if (stored is null || !stored.IsActive)
        {
            return new VerifyEmailResultDTO(
                false,
                "Invalid or expired verification code.",
                []);
        }

        stored.UsedAt = DateTime.UtcNow;
        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();

        return new VerifyEmailResultDTO(
            true,
            "Email verified successfully. You can now log in.",
            []);
    }

    public async Task<ResendVerificationResultDTO> ResendVerificationAsync(ResendVerificationRequestDTO dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            return new ResendVerificationResultDTO(
                true,
                "If an unverified account exists, a verification code was sent.");
        }

        if (user.EmailConfirmed)
        {
            return new ResendVerificationResultDTO(
                false,
                "Email is already verified.");
        }

        await InvalidateActiveCodesAsync(user.Id);
        await CreateAndSendVerificationCodeAsync(user);

        return new ResendVerificationResultDTO(
            true,
            "Verification code sent.");
    }

    public async Task<AuthTokensDTO?> RefreshSessionAsync(string rawRefreshToken)
    {
        var hash = _tokenService.HashToken(rawRefreshToken);

        var stored = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (stored is null || !stored.IsActive)
            return null;

        stored.RevokedAt = DateTime.UtcNow;
        return await IssueTokensAsync(stored.User);
    }

    public async Task LogoutSessionAsync(string rawRefreshToken)
    {
        var hash = _tokenService.HashToken(rawRefreshToken);

        var stored = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (stored is null || !stored.IsActive)
            return;

        stored.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private async Task CreateAndSendVerificationCodeAsync(ApplicationUser user)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        _context.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            UserId = user.Id,
            CodeHash = _tokenService.HashToken(code),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_sendGridSettings.VerificationCodeMins)
        });

        await _context.SaveChangesAsync();
        await _emailService.SendVerificationCodeAsync(user.Email!, code);
    }

    private async Task InvalidateActiveCodesAsync(int userId)
    {
        var activeCodes = await _context.EmailVerificationCodes
            .Where(c => c.UserId == userId && c.UsedAt == null && c.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var code in activeCodes)
            code.UsedAt = DateTime.UtcNow;

        if (activeCodes.Count > 0)
            await _context.SaveChangesAsync();
    }

    private async Task<AuthTokensDTO> IssueTokensAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user, roles);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(rawRefreshToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
        });

        await _context.SaveChangesAsync();

        return new AuthTokensDTO(accessToken, rawRefreshToken, expiresAt);
    }
}
