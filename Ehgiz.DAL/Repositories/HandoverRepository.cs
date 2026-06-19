using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class HandoverRepository : Repository<Handover>, IHandoverRepository
{
    public HandoverRepository(EhgizDbContext context) : base(context)
    {
    }

    public Task<Handover?> GetPendingHandoverAsync(int bookingId, HandoverType type)
        => _context.Handovers
            .Include(h => h.Images)
            .FirstOrDefaultAsync(h =>
                h.BookingId == bookingId &&
                h.Type == type &&
                h.IsAccepted == null);
}
