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
            b.Status != BookingStatus.Completed &&
            startDate < b.EndDate &&
            endDate > b.StartDate);

    public async Task<Booking?> GetBookingWithDetailsAsync(int bookingId)
        => await _context.Bookings
            .Include(b => b.Tool).ThenInclude(t => t.Owner)
            .Include(b => b.Tool).ThenInclude(t => t.Images)
            .Include(b => b.Renter)
            .Include(b => b.Payment)
            .Include(b => b.Handovers).ThenInclude(h => h.Images)
            .Include(b => b.Handovers).ThenInclude(h => h.SubmittedByUser)
            .Include(b => b.Handovers).ThenInclude(h => h.RespondedByUser)
            .Include(b => b.IssueReports).ThenInclude(ir => ir.Reporter)
            .Include(b => b.Reviews)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

    public async Task<IReadOnlyList<Booking>> GetBookedDatesByToolIdAsync(int toolId, DateTime from, DateTime to)
        => await _context.Bookings
            .AsNoTracking()
            .Where(b =>
                b.ToolId == toolId &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Rejected &&
                b.Status != BookingStatus.Completed &&
                b.StartDate < to &&
                b.EndDate > from)
            .Select(b => new Booking
            {
                Id = b.Id,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                Status = b.Status
            })
            .OrderBy(b => b.StartDate)
            .ToListAsync();

    public async Task<IReadOnlyList<Booking>> GetDisputedBookingsAsync()
        => await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Disputed)
            .Include(b => b.Tool).ThenInclude(t => t.Owner)
            .Include(b => b.Tool).ThenInclude(t => t.Images)
            .Include(b => b.Renter)
            .Include(b => b.Payment)
            .Include(b => b.IssueReports).ThenInclude(ir => ir.Reporter)
            .Include(b => b.Handovers).ThenInclude(h => h.Images)
            .Include(b => b.Handovers).ThenInclude(h => h.SubmittedByUser)
            .Include(b => b.Handovers).ThenInclude(h => h.RespondedByUser)
            .Include(b => b.Reviews)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
}
