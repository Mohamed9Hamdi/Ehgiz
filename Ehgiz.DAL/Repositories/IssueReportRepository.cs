using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class IssueReportRepository : Repository<IssueReport>, IIssueReportRepository
{
    public IssueReportRepository(EhgizDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<IssueReport>> GetAllWithDetailsAsync()
        => await _context.IssueReports
            .Include(ir => ir.Reporter)
            .AsNoTracking()
            .OrderByDescending(ir => ir.CreatedAt)
            .ToListAsync();

    public async Task<IssueReport?> GetByIdWithDetailsAsync(int id)
        => await _context.IssueReports
            .Include(ir => ir.Reporter)
            .FirstOrDefaultAsync(ir => ir.Id == id);
}
