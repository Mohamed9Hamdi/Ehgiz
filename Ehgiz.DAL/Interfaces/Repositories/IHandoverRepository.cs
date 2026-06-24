using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IHandoverRepository : IRepository<Handover>
{
    Task<Handover?> GetPendingHandoverAsync(int bookingId, HandoverType type);
}
