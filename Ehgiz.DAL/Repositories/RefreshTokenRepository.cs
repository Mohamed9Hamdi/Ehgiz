using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    private readonly EhgizDbContext _context;

    public RefreshTokenRepository(EhgizDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
    }

    public async Task<RefreshToken?> GetByHashWithUserAsync(string tokenHash)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
    }
}
