using Ehgiz.Application.DTOs.Auth;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Services;
using Ehgiz.Application.Settings;
using Ehgiz.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Ehgiz.Tests.Services;

public class AuthServiceTests : IAsyncLifetime
{
    private const string Password = "passw0rd1!";

    private readonly TestDb _db = new();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly ICloudinaryService _cloudinary = Substitute.For<ICloudinaryService>();
    private readonly TokenService _tokenService;
    private AuthService _sut = null!;

    private string? _lastVerificationCode;
    private string? _lastResetCode;

    public AuthServiceTests()
    {
        _tokenService = new TokenService(Options.Create(new JwtSettings
        {
            Key = "unit-test-signing-key-with-at-least-32-chars!",
            Issuer = "tests",
            Audience = "tests",
            AccessTokenMins = 15,
            RefreshTokenDays = 7
        }));

        _email.SendVerificationCodeAsync(Arg.Any<string>(), Arg.Do<string>(c => _lastVerificationCode = c))
            .Returns(Task.CompletedTask);
        _email.SendPasswordResetCodeAsync(Arg.Any<string>(), Arg.Do<string>(c => _lastResetCode = c))
            .Returns(Task.CompletedTask);
    }

    public ValueTask InitializeAsync()
    {
        _sut = new AuthService(
            _db.UserManager,
            _db.SignInManager,
            _db.Uow,
            _tokenService,
            _email,
            _notifications,
            _cloudinary,
            NullLogger<AuthService>.Instance,
            Options.Create(new JwtSettings { Key = "k", Issuer = "i", Audience = "a", AccessTokenMins = 15, RefreshTokenDays = 7 }),
            Options.Create(new SendGridSettings { ApiKey = "x", SenderEmail = "no-reply@test.local", VerificationCodeMins = 15 }));
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private static RegisterRequestDTO Register(string email = "new@test.local") =>
        new("New User", email, "0100000000", "Cairo", Password);

    // ── RegisterAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task Register_FailsWhenEmailAlreadyRegistered()
    {
        await _db.CreateIdentityUserAsync("dup@test.local");

        var result = await _sut.RegisterAsync(Register("dup@test.local"));

        Assert.False(result.Succeeded);
        Assert.Contains("already registered", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_CreatesUserWalletVerificationCodeAndSendsEmail()
    {
        var result = await _sut.RegisterAsync(Register());

        Assert.True(result.Succeeded);
        var user = await _db.UserManager.FindByEmailAsync("new@test.local");
        Assert.NotNull(user);
        Assert.False(user!.EmailConfirmed);
        Assert.Contains("user", await _db.UserManager.GetRolesAsync(user));

        Assert.Single(_db.Context.Wallets.Where(w => w.UserId == user.Id).ToList());
        Assert.Single(_db.Context.EmailVerificationCodes.Where(c => c.UserId == user.Id).ToList());
        Assert.NotNull(_lastVerificationCode);
    }

    // ── LoginAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_FailsForUnknownEmail()
    {
        var result = await _sut.LoginAsync(new LoginRequestDTO("ghost@test.local", Password));

        Assert.Null(result.Tokens);
        Assert.Equal("Invalid email or password.", result.FailureMessage);
    }

    [Fact]
    public async Task Login_BlocksDeactivatedAccount()
    {
        await _db.CreateIdentityUserAsync("gone@test.local", isActive: false);

        var result = await _sut.LoginAsync(new LoginRequestDTO("gone@test.local", Password));

        Assert.Null(result.Tokens);
        Assert.Contains("deactivated", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_RequiresVerifiedEmail()
    {
        await _db.CreateIdentityUserAsync("unverified@test.local", emailConfirmed: false);

        var result = await _sut.LoginAsync(new LoginRequestDTO("unverified@test.local", Password));

        Assert.Null(result.Tokens);
        Assert.Contains("verify your email", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_FailsWithWrongPassword()
    {
        await _db.CreateIdentityUserAsync("known@test.local");

        var result = await _sut.LoginAsync(new LoginRequestDTO("known@test.local", "wrong-passw0rd"));

        Assert.Null(result.Tokens);
        Assert.Equal("Invalid email or password.", result.FailureMessage);
    }

    [Fact]
    public async Task Login_IssuesAccessAndRefreshTokensOnSuccess()
    {
        var user = await _db.CreateIdentityUserAsync("ok@test.local");

        var result = await _sut.LoginAsync(new LoginRequestDTO("ok@test.local", Password));

        Assert.Null(result.FailureMessage);
        Assert.NotNull(result.Tokens);
        Assert.NotEmpty(result.Tokens!.AccessToken);
        Assert.Contains("user", result.Tokens.Roles);

        var stored = Assert.Single(_db.Context.RefreshTokens.Where(t => t.UserId == user.Id).ToList());
        Assert.Equal(_tokenService.HashToken(result.Tokens.RawRefreshToken), stored.TokenHash);
        Assert.Null(stored.RevokedAt);
    }

    // ── VerifyEmailAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyEmail_ConfirmsEmailWithCorrectCode()
    {
        await _sut.RegisterAsync(Register());

        var result = await _sut.VerifyEmailAsync(new VerifyEmailRequestDTO("new@test.local", _lastVerificationCode!));

        Assert.True(result.Succeeded);
        var user = await _db.UserManager.FindByEmailAsync("new@test.local");
        Assert.True(user!.EmailConfirmed);
    }

    [Fact]
    public async Task VerifyEmail_RejectsWrongCode()
    {
        await _sut.RegisterAsync(Register());

        var result = await _sut.VerifyEmailAsync(new VerifyEmailRequestDTO("new@test.local", "000000"));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task VerifyEmail_ReportsAlreadyVerified()
    {
        await _db.CreateIdentityUserAsync("done@test.local");

        var result = await _sut.VerifyEmailAsync(new VerifyEmailRequestDTO("done@test.local", "123456"));

        Assert.True(result.Succeeded);
        Assert.Contains("already verified", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── ForgotPassword / ResetPassword ──────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_ReturnsGenericSuccessWithoutRevealingAccountExistence()
    {
        var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequestDTO("ghost@test.local"));

        Assert.True(result.Succeeded);
        await _email.DidNotReceiveWithAnyArgs().SendPasswordResetCodeAsync(default!, default!);
    }

    [Fact]
    public async Task ForgotPassword_SendsCodeToVerifiedUser()
    {
        var user = await _db.CreateIdentityUserAsync("reset@test.local");

        await _sut.ForgotPasswordAsync(new ForgotPasswordRequestDTO("reset@test.local"));

        Assert.NotNull(_lastResetCode);
        Assert.Single(_db.Context.PasswordResetCodes.Where(c => c.UserId == user.Id).ToList());
    }

    [Fact]
    public async Task ForgotPassword_RateLimitsAfterThreeRequestsInWindow()
    {
        await _db.CreateIdentityUserAsync("limited@test.local");

        for (var i = 0; i < 4; i++)
            await _sut.ForgotPasswordAsync(new ForgotPasswordRequestDTO("limited@test.local"));

        await _email.Received(3).SendPasswordResetCodeAsync("limited@test.local", Arg.Any<string>());
    }

    [Fact]
    public async Task ResetPassword_SucceedsWithCorrectCodeAndRevokesRefreshTokens()
    {
        await _db.CreateIdentityUserAsync("victim@test.local");
        await _sut.LoginAsync(new LoginRequestDTO("victim@test.local", Password)); // creates refresh token
        await _sut.ForgotPasswordAsync(new ForgotPasswordRequestDTO("victim@test.local"));

        var result = await _sut.ResetPasswordAsync(
            new ResetPasswordRequestDTO("victim@test.local", _lastResetCode!, "brand-new-pw1!"));

        Assert.True(result.Succeeded);

        // Old refresh tokens are revoked
        Assert.All(_db.Context.RefreshTokens.ToList(), t => Assert.NotNull(t.RevokedAt));

        // New password works, old one does not
        var login = await _sut.LoginAsync(new LoginRequestDTO("victim@test.local", "brand-new-pw1!"));
        Assert.NotNull(login.Tokens);
        var oldLogin = await _sut.LoginAsync(new LoginRequestDTO("victim@test.local", Password));
        Assert.Null(oldLogin.Tokens);
    }

    [Fact]
    public async Task ResetPassword_WrongCodeCountsAttemptAndBurnsCodeAfterFiveTries()
    {
        var user = await _db.CreateIdentityUserAsync("attempts@test.local");
        await _sut.ForgotPasswordAsync(new ForgotPasswordRequestDTO("attempts@test.local"));

        for (var i = 0; i < 5; i++)
        {
            var attempt = await _sut.ResetPasswordAsync(
                new ResetPasswordRequestDTO("attempts@test.local", "000000", "whatever-pw1!"));
            Assert.False(attempt.Succeeded);
        }

        var code = _db.Context.PasswordResetCodes.Single(c => c.UserId == user.Id);
        Assert.Equal(5, code.AttemptCount);
        Assert.NotNull(code.UsedAt); // burned

        // Even the correct code no longer works once burned
        var final = await _sut.ResetPasswordAsync(
            new ResetPasswordRequestDTO("attempts@test.local", _lastResetCode!, "whatever-pw1!"));
        Assert.False(final.Succeeded);
    }

    // ── Refresh / Logout ────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshSession_RotatesRefreshToken()
    {
        await _db.CreateIdentityUserAsync("rotate@test.local");
        var login = await _sut.LoginAsync(new LoginRequestDTO("rotate@test.local", Password));

        var refreshed = await _sut.RefreshSessionAsync(login.Tokens!.RawRefreshToken);

        Assert.NotNull(refreshed);
        Assert.NotEqual(login.Tokens.RawRefreshToken, refreshed!.RawRefreshToken);

        // Old token is revoked and cannot be reused
        Assert.Null(await _sut.RefreshSessionAsync(login.Tokens.RawRefreshToken));
    }

    [Fact]
    public async Task RefreshSession_ReturnsNullForUnknownToken()
    {
        Assert.Null(await _sut.RefreshSessionAsync("not-a-real-token"));
    }

    [Fact]
    public async Task LogoutSession_RevokesRefreshToken()
    {
        await _db.CreateIdentityUserAsync("bye@test.local");
        var login = await _sut.LoginAsync(new LoginRequestDTO("bye@test.local", Password));

        await _sut.LogoutSessionAsync(login.Tokens!.RawRefreshToken);

        Assert.Null(await _sut.RefreshSessionAsync(login.Tokens.RawRefreshToken));
    }
}
