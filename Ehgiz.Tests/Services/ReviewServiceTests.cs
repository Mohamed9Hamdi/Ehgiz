using System.ComponentModel.DataAnnotations;
using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.DTOs.Review;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Services;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.Tests.TestHelpers;
using NSubstitute;

namespace Ehgiz.Tests.Services;

public class ReviewServiceTests : IAsyncLifetime
{
    private readonly TestDb _db = new();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private ReviewService _sut = null!;
    private ApplicationUser _owner = null!;
    private ApplicationUser _renter = null!;
    private Tool _tool = null!;

    public async ValueTask InitializeAsync()
    {
        _sut = new ReviewService(_db.Uow, _notifications);
        _owner = await _db.SeedUserAsync(fullName: "Owner");
        _renter = await _db.SeedUserAsync(fullName: "Renter");
        var category = await _db.SeedCategoryAsync();
        _tool = await _db.SeedToolAsync(_owner.Id, category.Id, name: "Drill");
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task CreateAsync_RejectsOutOfRangeRating(int rating)
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(new CreateReviewDto { BookingId = 1, Rating = rating }, _renter.Id));
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenBookingMissing()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.CreateAsync(new CreateReviewDto { BookingId = 999, Rating = 5 }, _renter.Id));
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenReviewerIsNotRenter()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Completed);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreateAsync(new CreateReviewDto { BookingId = booking.Id, Rating = 5 }, _owner.Id));
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenBookingNotCompleted()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Active);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(new CreateReviewDto { BookingId = booking.Id, Rating = 5 }, _renter.Id));
    }

    [Fact]
    public async Task CreateAsync_AllowsReviewOnDisputeResolvedCancelledBooking()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Cancelled,
            adminResolutionNotes: "Resolved in renter's favor");

        var result = await _sut.CreateAsync(
            new CreateReviewDto { BookingId = booking.Id, Rating = 4, Comment = "Fair outcome" }, _renter.Id);

        Assert.Equal(4, result.Rating);
    }

    [Fact]
    public async Task CreateAsync_ThrowsOnDuplicateReview()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Completed);
        await _sut.CreateAsync(new CreateReviewDto { BookingId = booking.Id, Rating = 5 }, _renter.Id);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(new CreateReviewDto { BookingId = booking.Id, Rating = 3 }, _renter.Id));
    }

    [Fact]
    public async Task CreateAsync_PersistsReviewAndNotifiesOwner()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Completed);

        var result = await _sut.CreateAsync(
            new CreateReviewDto { BookingId = booking.Id, Rating = 5, Comment = "Great tool" }, _renter.Id);

        Assert.Equal(5, result.Rating);
        Assert.Equal("Great tool", result.Comment);
        Assert.Equal(_tool.Id, result.ToolId);
        Assert.Equal("Renter", result.RenterName);

        await _notifications.Received(1).CreateAsync(Arg.Is<CreateNotificationDto>(n =>
            n.UserId == _owner.Id && n.Type == NotificationType.Review));
    }

    [Fact]
    public async Task GetByToolAsync_ThrowsWhenToolMissing()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetByToolAsync(999));
    }

    [Fact]
    public async Task GetByToolAsync_ReturnsReviewsForTool()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Completed);
        await _sut.CreateAsync(new CreateReviewDto { BookingId = booking.Id, Rating = 4 }, _renter.Id);

        var result = await _sut.GetByToolAsync(_tool.Id);

        Assert.Equal(4, Assert.Single(result).Rating);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenNotReviewAuthor()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Completed);
        var review = await _sut.CreateAsync(new CreateReviewDto { BookingId = booking.Id, Rating = 5 }, _renter.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.DeleteAsync(review.Id, _owner.Id));
    }

    [Fact]
    public async Task DeleteAsync_RemovesOwnReview()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Completed);
        var review = await _sut.CreateAsync(new CreateReviewDto { BookingId = booking.Id, Rating = 5 }, _renter.Id);

        await _sut.DeleteAsync(review.Id, _renter.Id);

        Assert.Empty(await _sut.GetByToolAsync(_tool.Id));
    }

    [Fact]
    public async Task GetAverageRatingAsync_ReturnsZeroWithoutReviews()
    {
        Assert.Equal(0, await _sut.GetAverageRatingAsync(_tool.Id));
    }

    [Fact]
    public async Task GetAverageRatingAsync_RoundsToOneDecimal()
    {
        var renter2 = await _db.SeedUserAsync();
        var b1 = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Completed);
        var b2 = await _db.SeedBookingAsync(_tool.Id, renter2.Id, BookingStatus.Completed);
        var b3 = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Completed);

        await _sut.CreateAsync(new CreateReviewDto { BookingId = b1.Id, Rating = 5 }, _renter.Id);
        await _sut.CreateAsync(new CreateReviewDto { BookingId = b2.Id, Rating = 4 }, renter2.Id);
        await _sut.CreateAsync(new CreateReviewDto { BookingId = b3.Id, Rating = 4 }, _renter.Id);

        Assert.Equal(4.3, await _sut.GetAverageRatingAsync(_tool.Id));
    }
}
