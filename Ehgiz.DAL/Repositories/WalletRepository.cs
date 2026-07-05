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

    public async Task<bool> TryDebitBalanceAsync(int walletId, decimal amount)
    {
        var affected = await _context.Wallets
            .Where(w => w.Id == walletId && w.Balance >= amount)
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.Balance, w => w.Balance - amount)
                .SetProperty(w => w.UpdatedAt, DateTime.UtcNow));

        return affected > 0;
    }

    public async Task<bool> TryHoldBalanceAsync(int walletId, decimal amount)
    {
        var affected = await _context.Wallets
            .Where(w => w.Id == walletId && w.Balance >= amount)
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.Balance, w => w.Balance - amount)
                .SetProperty(w => w.HeldBalance, w => w.HeldBalance + amount)
                .SetProperty(w => w.UpdatedAt, DateTime.UtcNow));

        return affected > 0;
    }
}
