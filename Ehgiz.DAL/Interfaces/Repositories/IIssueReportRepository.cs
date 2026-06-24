using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IIssueReportRepository : IRepository<IssueReport>
{
    Task<IReadOnlyList<IssueReport>> GetAllWithDetailsAsync();
    Task<IssueReport?> GetByIdWithDetailsAsync(int id);
}
