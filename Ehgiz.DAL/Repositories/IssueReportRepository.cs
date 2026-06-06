using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;

namespace Ehgiz.DAL.Repositories;

public class IssueReportRepository : Repository<IssueReport>, IIssueReportRepository
{
    public IssueReportRepository(EhgizDbContext context) : base(context)
    {
    }
}
