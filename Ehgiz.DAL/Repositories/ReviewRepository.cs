using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class ReviewRepository : Repository<Review>, IReviewRepository
{
    public ReviewRepository(EhgizDbContext context) : base(context)
    {
    }

    public Task<bool> ExistsForBookingAsync(int bookingId)
        => _context.Reviews.AnyAsync(r => r.BookingId == bookingId);

    public Task<List<int>> GetRatingsByToolAsync(int toolId)
        => _context.Reviews
            .Where(r => r.Booking.ToolId == toolId)
            .Select(r => r.Rating)
            .ToListAsync();
}
