using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IEmailVerificationCodeRepository : IRepository<EmailVerificationCode>
{
    Task<EmailVerificationCode?> GetByUserAndHashAsync(int userId, string codeHash);
    Task<IReadOnlyList<EmailVerificationCode>> GetActiveByUserIdAsync(int userId);
}
