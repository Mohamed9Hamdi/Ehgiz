using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class UserConnectionRepository : Repository<UserConnection>, IUserConnectionRepository
{
    private readonly EhgizDbContext _context;

    public UserConnectionRepository(EhgizDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserConnection>> GetByUserIdAsync(int userId)
    {
        return await _context.UserConnections
            .Where(uc => uc.UserId == userId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<UserConnection?> GetByConnectionIdAsync(string connectionId)
    {
        return await _context.UserConnections
            .FirstOrDefaultAsync(uc => uc.ConnectionId == connectionId);
    }

    public async Task RemoveByConnectionIdAsync(string connectionId)
    {
        await _context.UserConnections
            .Where(uc => uc.ConnectionId == connectionId)
            .ExecuteDeleteAsync();
    }

    public async Task RemoveAllByUserIdAsync(int userId)
    {
        await _context.UserConnections
            .Where(uc => uc.UserId == userId)
            .ExecuteDeleteAsync();
    }
}
