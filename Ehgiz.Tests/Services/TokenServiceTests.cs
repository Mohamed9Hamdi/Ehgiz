using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ehgiz.Application.Services;
using Ehgiz.Application.Settings;
using Ehgiz.DAL.Entities;
using Microsoft.Extensions.Options;

namespace Ehgiz.Tests.Services;

public class TokenServiceTests
{
    private static readonly JwtSettings Settings = new()
    {
        Key = "unit-test-signing-key-with-at-least-32-chars!",
        Issuer = "ehgiz-tests",
        Audience = "ehgiz-clients",
        AccessTokenMins = 15,
        RefreshTokenDays = 7
    };

    private readonly TokenService _sut = new(Options.Create(Settings));

    private static ApplicationUser User => new()
    {
        Id = 42,
        Email = "renter@test.local",
        FullName = "Renter One"
    };

    [Fact]
    public void GenerateAccessToken_EmbedsUserClaimsAndRoles()
    {
        var (token, _) = _sut.GenerateAccessToken(User, ["user", "admin"]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(Settings.Issuer, jwt.Issuer);
        Assert.Contains(Settings.Audience, jwt.Audiences);
        Assert.Equal("42", jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("renter@test.local", jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Renter One", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);

        var roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Equal(["user", "admin"], roles);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresAfterConfiguredMinutes()
    {
        var before = DateTime.UtcNow;
        var (_, expiresAt) = _sut.GenerateAccessToken(User, []);

        Assert.InRange(expiresAt, before.AddMinutes(15), DateTime.UtcNow.AddMinutes(15).AddSeconds(1));
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueBase64Tokens()
    {
        var first = _sut.GenerateRefreshToken();
        var second = _sut.GenerateRefreshToken();

        Assert.NotEqual(first, second);
        Assert.Equal(64, Convert.FromBase64String(first).Length);
    }

    [Fact]
    public void HashToken_IsDeterministicAndDoesNotLeakInput()
    {
        var raw = _sut.GenerateRefreshToken();

        var hash1 = _sut.HashToken(raw);
        var hash2 = _sut.HashToken(raw);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // SHA-256 as hex
        Assert.NotEqual(raw, hash1);
        Assert.NotEqual(hash1, _sut.HashToken(raw + "x"));
    }
}
