using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class BookingRepository : Repository<Booking>, IBookingRepository
{
    public BookingRepository(EhgizDbContext context) : base(context)
    {
    }

    public Task<bool> HasOverlappingBookingAsync(int toolId, DateTime startDate, DateTime endDate)
        => _context.Bookings.AnyAsync(b =>
            b.ToolId == toolId &&
            b.Status != BookingStatus.Cancelled &&
            b.Status != BookingStatus.Rejected &&
            startDate < b.EndDate &&
            endDate > b.StartDate);
}
