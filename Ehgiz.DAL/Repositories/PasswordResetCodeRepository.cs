using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class PasswordResetCodeRepository : Repository<PasswordResetCode>, IPasswordResetCodeRepository
{
    public PasswordResetCodeRepository(EhgizDbContext context) : base(context)
    {
    }

    public async Task<PasswordResetCode?> GetByUserAndHashAsync(int userId, string codeHash)
    {
        return await _context.PasswordResetCodes
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CodeHash == codeHash);
    }

    public async Task<IReadOnlyList<PasswordResetCode>> GetActiveByUserIdAsync(int userId)
    {
        return await _context.PasswordResetCodes
            .Where(c => c.UserId == userId && c.UsedAt == null && c.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }
}
