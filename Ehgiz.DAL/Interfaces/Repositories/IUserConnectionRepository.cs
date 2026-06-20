using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IUserConnectionRepository : IRepository<UserConnection>
{
    Task<IReadOnlyList<UserConnection>> GetByUserIdAsync(int userId);
    Task<UserConnection?> GetByConnectionIdAsync(string connectionId);
    Task RemoveByConnectionIdAsync(string connectionId);
    Task RemoveAllByUserIdAsync(int userId);
}
