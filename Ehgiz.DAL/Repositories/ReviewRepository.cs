using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class ReviewRepository : Repository<Review>, IReviewRepository
{
    private readonly EhgizDbContext _context;

    public ReviewRepository(EhgizDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<Review>> GetByToolAsync(int toolId)
    {
        return await _context.Reviews
            .Include(r => r.Booking)
                .ThenInclude(b => b.Tool)
            .Include(r => r.Booking)
                .ThenInclude(b => b.Renter)
            .AsNoTracking()
            .Where(r => r.Booking.ToolId == toolId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Review?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Reviews
            .Include(r => r.Booking)
                .ThenInclude(b => b.Tool)
            .Include(r => r.Booking)
                .ThenInclude(b => b.Renter)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<bool> ExistsForBookingAsync(int bookingId)
    {
        return await _context.Reviews.AnyAsync(r => r.BookingId == bookingId);
    }

    public async Task<List<int>> GetRatingsByToolAsync(int toolId)
    {
        return await _context.Reviews
            .Include(r => r.Booking)
            .AsNoTracking()
            .Where(r => r.Booking.ToolId == toolId)
            .Select(r => r.Rating)
            .ToListAsync();
    }
}
