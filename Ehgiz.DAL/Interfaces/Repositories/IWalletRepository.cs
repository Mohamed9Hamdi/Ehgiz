using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IWalletRepository : IRepository<Wallet>
{
    Task<Wallet?> GetByUserIdAsync(int userId);
    Task<Wallet> GetOrCreateByUserIdAsync(int userId);

    /// <summary>
    /// Atomically deducts <paramref name="amount"/> from the wallet's balance
    /// only if the balance covers it. Returns false when it does not, so
    /// concurrent withdrawals cannot overdraw via a read-check-write race.
    /// </summary>
    Task<bool> TryDebitBalanceAsync(int walletId, decimal amount);

    /// <summary>
    /// Atomically moves <paramref name="amount"/> from the wallet's available
    /// balance into its held (escrow) balance, only if the balance covers it.
    /// Returns false when it does not.
    /// </summary>
    Task<bool> TryHoldBalanceAsync(int walletId, decimal amount);
}
