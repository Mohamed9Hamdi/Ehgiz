using System.Security.Cryptography;
using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Auth;
using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Settings;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ehgiz.Application.Services;

public class AuthService : IAuthService
{
    private const int MaxResetCodeAttempts = 5;
    private const int MaxResetRequestsPerWindow = 3;
    private static readonly TimeSpan ResetRequestWindow = TimeSpan.FromMinutes(15);
    private const string GenericResetCodeMessage = "If an account exists, a password reset code was sent.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUnitOfWork _uow;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<AuthService> _logger;
    private readonly JwtSettings _jwtSettings;
    private readonly SendGridSettings _sendGridSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUnitOfWork uow,
        ITokenService tokenService,
        IEmailService emailService,
        INotificationService notificationService,
        ICloudinaryService cloudinaryService,
        ILogger<AuthService> logger,
        IOptions<JwtSettings> jwtSettings,
        IOptions<SendGridSettings> sendGridSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _uow = uow;
        _tokenService = tokenService;
        _emailService = emailService;
        _notificationService = notificationService;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
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

        await UploadRegistrationImagesAsync(user, dto);

        try
        {
            await _uow.Wallets.GetOrCreateByUserIdAsync(user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create wallet for new user {UserId}", user.Id);
        }

        await CreateAndSendVerificationCodeAsync(user);

        try
        {
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = user.Id,
                Title = "Welcome to Ehgiz!",
                Message = "Your account has been created. Please verify your email to get started.",
                Type = NotificationType.System
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create registration notification for user {UserId}", user.Id);
        }

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

        if (!user.IsActive)
            return new AuthLoginResultDTO(null, "Your account has been deactivated. Please contact support.");

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (signInResult.Succeeded)
        {
            var tokens = await IssueTokensAsync(user);

            try
            {
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    UserId = user.Id,
                    Title = "New Login",
                    Message = "You have successfully logged in.",
                    Type = NotificationType.System
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create login notification for user {UserId}", user.Id);
            }

            return new AuthLoginResultDTO(tokens, null);
        }

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
        var stored = await _uow.EmailVerificationCodes.GetByUserAndHashAsync(user.Id, hash);

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
        await _uow.SaveChangesAsync();

        try
        {
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = user.Id,
                Title = "Email Verified",
                Message = "Your email address has been verified. You can now log in.",
                Type = NotificationType.System
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create email verification notification for user {UserId}", user.Id);
        }

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

    public async Task<ForgotPasswordResultDTO> ForgotPasswordAsync(ForgotPasswordRequestDTO dto)
    {
        // Always return the same generic success so responses never reveal
        // whether an account exists (or is unverified / rate limited).
        await IssueResetCodeIfAllowedAsync(dto.Email);

        return new ForgotPasswordResultDTO(true, GenericResetCodeMessage, []);
    }

    public async Task<ResendResetCodeResultDTO> ResendResetCodeAsync(ResendResetCodeRequestDTO dto)
    {
        await IssueResetCodeIfAllowedAsync(dto.Email);

        return new ResendResetCodeResultDTO(true, GenericResetCodeMessage, []);
    }

    private async Task IssueResetCodeIfAllowedAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.EmailConfirmed)
            return;

        var windowStart = DateTime.UtcNow.Subtract(ResetRequestWindow);
        var recentRequests = await _uow.PasswordResetCodes.CountAsync(c =>
            c.UserId == user.Id && c.CreatedAt >= windowStart);

        if (recentRequests >= MaxResetRequestsPerWindow)
        {
            _logger.LogWarning("Password reset rate limit reached for user {UserId}", user.Id);
            return;
        }

        await InvalidateActiveResetCodesAsync(user.Id);
        await CreateAndSendPasswordResetCodeAsync(user);
    }

    public async Task<ResetPasswordResultDTO> ResetPasswordAsync(ResetPasswordRequestDTO dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            return new ResetPasswordResultDTO(
                false,
                "Invalid or expired reset code.",
                []);
        }

        var hash = _tokenService.HashToken(dto.Code.Trim());
        var activeCodes = await _uow.PasswordResetCodes.GetActiveByUserIdAsync(user.Id);
        var stored = activeCodes.FirstOrDefault(c => c.CodeHash == hash);

        if (stored is null)
        {
            // Wrong code: count the attempt against the user's active code(s)
            // and burn them once the attempt budget is spent.
            foreach (var code in activeCodes)
            {
                code.AttemptCount++;
                if (code.AttemptCount >= MaxResetCodeAttempts)
                    code.UsedAt = DateTime.UtcNow;
            }

            if (activeCodes.Count > 0)
                await _uow.SaveChangesAsync();

            return new ResetPasswordResultDTO(
                false,
                "Invalid or expired reset code.",
                []);
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);
        if (!result.Succeeded)
        {
            return new ResetPasswordResultDTO(
                false,
                "Password reset failed.",
                result.Errors.Select(e => e.Description));
        }

        stored.UsedAt = DateTime.UtcNow;
        await _uow.RefreshTokens.RevokeAllActiveByUserIdAsync(user.Id);
        await _uow.SaveChangesAsync();

        try
        {
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = user.Id,
                Title = "Password Changed",
                Message = "Your password was reset successfully.",
                Type = NotificationType.System
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create password reset notification for user {UserId}", user.Id);
        }

        return new ResetPasswordResultDTO(
            true,
            "Password reset successfully. You can now log in.",
            []);
    }

    public async Task<AuthTokensDTO?> RefreshSessionAsync(string rawRefreshToken)
    {
        var hash = _tokenService.HashToken(rawRefreshToken);

        var stored = await _uow.RefreshTokens.GetByHashWithUserAsync(hash);

        if (stored is null || !stored.IsActive || !stored.User.IsActive)
            return null;

        stored.RevokedAt = DateTime.UtcNow;
        return await IssueTokensAsync(stored.User);
    }

    public async Task LogoutSessionAsync(string rawRefreshToken)
    {
        var hash = _tokenService.HashToken(rawRefreshToken);

        var stored = await _uow.RefreshTokens.GetByHashAsync(hash);

        if (stored is null || !stored.IsActive)
            return;

        stored.RevokedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
    }

    private async Task UploadRegistrationImagesAsync(ApplicationUser user, RegisterRequestDTO dto)
    {
        if (dto.ProfileImage is null && dto.NationalIdImage is null)
            return;

        try
        {
            if (dto.ProfileImage is not null)
            {
                var upload = await _cloudinaryService.UploadImageAsync(dto.ProfileImage);
                user.ProfileImageUrl = upload.ImageUrl;
                user.ProfileImagePublicId = upload.PublicId;
            }

            if (dto.NationalIdImage is not null)
            {
                var upload = await _cloudinaryService.UploadImageAsync(dto.NationalIdImage);
                user.NationalIdImageUrl = upload.ImageUrl;
                user.NationalIdImagePublicId = upload.PublicId;
            }

            await _userManager.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to upload registration images for user {UserId}", user.Id);
        }
    }

    private async Task CreateAndSendVerificationCodeAsync(ApplicationUser user)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        await _uow.EmailVerificationCodes.AddAsync(new EmailVerificationCode
        {
            UserId = user.Id,
            CodeHash = _tokenService.HashToken(code),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_sendGridSettings.VerificationCodeMins)
        });

        await _uow.SaveChangesAsync();
        await _emailService.SendVerificationCodeAsync(user.Email!, code);
    }

    private async Task CreateAndSendPasswordResetCodeAsync(ApplicationUser user)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        await _uow.PasswordResetCodes.AddAsync(new PasswordResetCode
        {
            UserId = user.Id,
            CodeHash = _tokenService.HashToken(code),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_sendGridSettings.VerificationCodeMins)
        });

        await _uow.SaveChangesAsync();
        await _emailService.SendPasswordResetCodeAsync(user.Email!, code);
    }

    private async Task InvalidateActiveResetCodesAsync(int userId)
    {
        var activeCodes = await _uow.PasswordResetCodes.GetActiveByUserIdAsync(userId);

        foreach (var code in activeCodes)
            code.UsedAt = DateTime.UtcNow;

        if (activeCodes.Count > 0)
            await _uow.SaveChangesAsync();
    }

    private async Task InvalidateActiveCodesAsync(int userId)
    {
        var activeCodes = await _uow.EmailVerificationCodes.GetActiveByUserIdAsync(userId);

        foreach (var code in activeCodes)
            code.UsedAt = DateTime.UtcNow;

        if (activeCodes.Count > 0)
            await _uow.SaveChangesAsync();
    }

    private async Task<AuthTokensDTO> IssueTokensAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user, roles);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();

        await _uow.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(rawRefreshToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
        });

        await _uow.SaveChangesAsync();

        return new AuthTokensDTO(accessToken, rawRefreshToken, expiresAt, roles);
    }
}
