using Ehgiz.Application.DTOs.Auth;
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
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        EhgizDbContext context,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
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

        return new RegisterResultDTO(
            true,
            user.Id.ToString(),
            "Account created successfully. Please log in.",
            []);
    }

    public async Task<AuthTokensDTO?> LoginAsync(LoginRequestDTO dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return null;

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
            return null;

        return await IssueTokensAsync(user);
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

    private async Task<AuthTokensDTO> IssueTokensAsync(ApplicationUser user)
    {
        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);
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
