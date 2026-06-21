using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class EmailVerificationCodeRepository : Repository<EmailVerificationCode>, IEmailVerificationCodeRepository
{
    private readonly EhgizDbContext _context;

    public EmailVerificationCodeRepository(EhgizDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<EmailVerificationCode?> GetByUserAndHashAsync(int userId, string codeHash)
    {
        return await _context.EmailVerificationCodes
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CodeHash == codeHash);
    }

    public async Task<IReadOnlyList<EmailVerificationCode>> GetActiveByUserIdAsync(int userId)
    {
        return await _context.EmailVerificationCodes
            .Where(c => c.UserId == userId && c.UsedAt == null && c.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }
}
