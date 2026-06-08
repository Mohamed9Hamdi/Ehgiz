namespace Ehgiz.Application.Settings;

public class JwtSettings
{
    public string Key { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int AccessTokenMins { get; set; }
    public int RefreshTokenDays { get; set; }
}
