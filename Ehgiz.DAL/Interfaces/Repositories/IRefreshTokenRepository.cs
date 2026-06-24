using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash);
    Task<RefreshToken?> GetByHashWithUserAsync(string tokenHash);
    Task RevokeAllActiveByUserIdAsync(int userId);
}
