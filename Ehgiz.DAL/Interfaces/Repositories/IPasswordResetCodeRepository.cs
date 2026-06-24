using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IPasswordResetCodeRepository : IRepository<PasswordResetCode>
{
    Task<PasswordResetCode?> GetByUserAndHashAsync(int userId, string codeHash);
    Task<IReadOnlyList<PasswordResetCode>> GetActiveByUserIdAsync(int userId);
}
