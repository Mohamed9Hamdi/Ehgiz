using System.Linq.Expressions;
using Ehgiz.DAL.Data;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly EhgizDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(EhgizDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public IQueryable<T> Query() => _dbSet.AsNoTracking();

    public async Task<T?> GetByIdAsync(object id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }


    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }


    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }



    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return predicate is null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(predicate);
    }
}
