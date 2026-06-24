using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.DTOs.Review;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Mapster;
using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;

    public ReviewService(IUnitOfWork uow, INotificationService notificationService)
    {
        _uow = uow;
        _notificationService = notificationService;
    }

    public async Task<List<ReviewDto>> GetByToolAsync(int toolId)
    {
        var toolExists = await _uow.Tools.CountAsync(t => t.Id == toolId) > 0;
        if (!toolExists)
            throw new KeyNotFoundException($"Tool with id {toolId} not found");

        var reviews = await _uow.Reviews.GetByToolAsync(toolId);
        return reviews.Adapt<List<ReviewDto>>();
    }

    public async Task<ReviewDto> GetByIdAsync(int id)
    {
        var review = await _uow.Reviews.GetByIdWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Review with id {id} not found");

        return review.Adapt<ReviewDto>();
    }

    public async Task<ReviewDto> CreateAsync(CreateReviewDto dto, int renterId)
    {
        if (dto.Rating is < 1 or > 5)
            throw new ValidationException("Rating must be between 1 and 5");

        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(dto.BookingId)
            ?? throw new KeyNotFoundException($"Booking {dto.BookingId} not found");

        if (booking.RenterId != renterId)
            throw new UnauthorizedAccessException("You can only review your own bookings");

        var isCompleted = booking.Status == BookingStatus.Completed;
        var isDisputeResolvedForRenter = booking.Status == BookingStatus.Cancelled
                                         && !string.IsNullOrEmpty(booking.AdminResolutionNotes);
        if (!isCompleted && !isDisputeResolvedForRenter)
            throw new ValidationException("Reviews can only be left on completed or dispute-resolved bookings");

        var alreadyReviewed = await _uow.Reviews.ExistsForBookingAsync(dto.BookingId);
        if (alreadyReviewed)
            throw new ValidationException("You already reviewed this booking");

        var review = dto.Adapt<Review>();
        await _uow.Reviews.AddAsync(review);
        await _uow.SaveChangesAsync();

        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = booking.Tool.OwnerId,
            Title = "New Review Received",
            Message = $"Someone left a {dto.Rating}-star review on '{booking.Tool.Name}'.",
            Type = NotificationType.Review,
            Url = $"/tools/{booking.ToolId}"
        });

        return await GetByIdAsync(review.Id);
    }

    public async Task DeleteAsync(int id, int renterId)
    {
        var review = await _uow.Reviews.GetByIdWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Review {id} not found");

        if (review.Booking.RenterId != renterId)
            throw new UnauthorizedAccessException("You can only delete your own reviews");

        _uow.Reviews.Remove(review);
        await _uow.SaveChangesAsync();
    }

    public async Task<double> GetAverageRatingAsync(int toolId)
    {
        var ratings = await _uow.Reviews.GetRatingsByToolAsync(toolId);
        return ratings.Count == 0 ? 0 : Math.Round(ratings.Average(), 1);
    }
}
