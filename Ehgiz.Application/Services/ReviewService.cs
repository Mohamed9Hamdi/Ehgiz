using Ehgiz.Application.DTOs.Review;
using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

public class ReviewService : IReviewService
{
    private readonly EhgizDbContext _context;

    public ReviewService(EhgizDbContext context) => _context = context;

    public async Task<List<ReviewDto>> GetByToolAsync(int toolId)
    {
        var toolExists = await _context.Tools.AnyAsync(t => t.Id == toolId);

        if (!toolExists)
            throw new KeyNotFoundException($"Tool with id {toolId} not found");

        var reviews = await _context.Reviews
            .Include(r => r.Booking)
                .ThenInclude(b => b.Tool)
            .Include(r => r.Booking)
                .ThenInclude(b => b.Renter)
            .AsNoTracking()
            .Where(r => r.Booking.ToolId == toolId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reviews.Adapt<List<ReviewDto>>();
    }

    public async Task<ReviewDto> GetByIdAsync(int id)
    {
        var review = await _context.Reviews
            .Include(r => r.Booking)
                .ThenInclude(b => b.Tool)
            .Include(r => r.Booking)
                .ThenInclude(b => b.Renter)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
            throw new KeyNotFoundException($"Review with id {id} not found");

        return review.Adapt<ReviewDto>();
    }

    public async Task<ReviewDto> CreateAsync(CreateReviewDto dto, int renterId)
    {
        if (dto.Rating is < 1 or > 5)
            throw new ValidationException("Rating must be between 1 and 5");

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == dto.BookingId);

        if (booking == null)
            throw new KeyNotFoundException($"Booking {dto.BookingId} not found");

        if (booking.RenterId != renterId)
            throw new UnauthorizedAccessException("You can only review your own bookings");

        var alreadyReviewed = await _context.Reviews
            .AnyAsync(r => r.BookingId == dto.BookingId);

        if (alreadyReviewed)
            throw new ValidationException("You already reviewed this booking");

        var review = dto.Adapt<Review>();

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(review.Id);
    }

    public async Task DeleteAsync(int id, int renterId)
    {
        var review = await _context.Reviews
            .Include(r => r.Booking)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
            throw new KeyNotFoundException($"Review {id} not found");

        if (review.Booking.RenterId != renterId)
            throw new UnauthorizedAccessException("You can only delete your own reviews");

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
    }

    public async Task<double> GetAverageRatingAsync(int toolId)
    {
        var ratings = await _context.Reviews
            .Include(r => r.Booking)
            .Where(r => r.Booking.ToolId == toolId)
            .Select(r => r.Rating)
            .ToListAsync();

        return ratings.Count == 0 ? 0 : Math.Round(ratings.Average(), 1);
    }
}