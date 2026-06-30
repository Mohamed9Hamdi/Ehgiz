using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IWalletRepository : IRepository<Wallet>
{
    Task<Wallet?> GetByUserIdAsync(int userId);
    Task<Wallet> GetOrCreateByUserIdAsync(int userId);
}
