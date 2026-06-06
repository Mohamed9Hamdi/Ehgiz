using System.Linq.Expressions;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
}
