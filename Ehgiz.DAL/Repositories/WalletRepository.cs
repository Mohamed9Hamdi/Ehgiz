using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class WalletRepository : Repository<Wallet>, IWalletRepository
{
    private readonly EhgizDbContext _context;

    public WalletRepository(EhgizDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<Wallet?> GetByUserIdAsync(int userId) =>
        _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

    public Task<Wallet?> GetByUserIdWithTransactionsAsync(int userId) =>
        _context.Wallets
            .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt))
            .FirstOrDefaultAsync(w => w.UserId == userId);
}
