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

    public async Task<IReadOnlyList<Booking>> GetByRenterIdAsync(int renterId)
        => await _context.Bookings
            .AsNoTracking()
            .Where(b => b.RenterId == renterId)
            .Include(b => b.Tool).ThenInclude(t => t.Owner)
            .Include(b => b.Tool).ThenInclude(t => t.Images)
            .Include(b => b.Handovers).ThenInclude(h => h.Images)
            .Include(b => b.Reviews)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<Booking>> GetByOwnerIdAsync(int ownerId)
        => await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Tool.OwnerId == ownerId)
            .Include(b => b.Tool).ThenInclude(t => t.Images)
            .Include(b => b.Renter)
            .Include(b => b.Handovers).ThenInclude(h => h.Images)
            .Include(b => b.Reviews)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<Booking?> GetBookingWithDetailsAsync(int bookingId)
        => await _context.Bookings
            .Include(b => b.Tool).ThenInclude(t => t.Owner)
            .Include(b => b.Tool).ThenInclude(t => t.Images)
            .Include(b => b.Renter)
            .Include(b => b.Payment)
            .Include(b => b.Handovers).ThenInclude(h => h.Images)
            .Include(b => b.Handovers).ThenInclude(h => h.SubmittedByUser)
            .Include(b => b.Handovers).ThenInclude(h => h.RespondedByUser)
            .Include(b => b.Reviews)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
}
