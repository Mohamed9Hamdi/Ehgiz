using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class WalletRepository : Repository<Wallet>, IWalletRepository
{
    public WalletRepository(EhgizDbContext context) : base(context)
    {
    }

    public Task<Wallet?> GetByUserIdAsync(int userId) =>
        _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

    public async Task<Wallet> GetOrCreateByUserIdAsync(int userId)
    {
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet is not null) return wallet;

        wallet = new Wallet
        {
            UserId = userId,
            Balance = 0,
            HeldBalance = 0,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();
        return wallet;
    }

    public Task<Wallet?> GetByUserIdWithTransactionsAsync(int userId) =>
        _context.Wallets
            .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt))
            .FirstOrDefaultAsync(w => w.UserId == userId);
}
