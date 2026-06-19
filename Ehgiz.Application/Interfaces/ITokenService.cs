using Ehgiz.DAL.Entities;

namespace Ehgiz.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    string HashToken(string rawToken);
}
