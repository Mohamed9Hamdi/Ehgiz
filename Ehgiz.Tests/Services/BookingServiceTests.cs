using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.DTOs.Handovers;
using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Services;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.Tests.TestHelpers;
using NSubstitute;

namespace Ehgiz.Tests.Services;

public class BookingServiceTests : IAsyncLifetime
{
    private readonly TestDb _db = new();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly ICloudinaryService _cloudinary = Substitute.For<ICloudinaryService>();
    private BookingService _sut = null!;
    private ApplicationUser _owner = null!;
    private ApplicationUser _renter = null!;
    private Tool _tool = null!;

    private static DateTime Start => DateTime.UtcNow.Date.AddDays(2);
    private static DateTime End => DateTime.UtcNow.Date.AddDays(5); // 3 days

    public async ValueTask InitializeAsync()
    {
        _sut = new BookingService(_db.Uow, _cloudinary, _notifications);
        _owner = await _db.SeedUserAsync(fullName: "Owner");
        _renter = await _db.SeedUserAsync(fullName: "Renter");
        var category = await _db.SeedCategoryAsync();
        // 3 days × 10 = 30 rental + 20 insurance = 50 total, default fee 10% = 3
        _tool = await _db.SeedToolAsync(_owner.Id, category.Id, pricePerDay: 10m, insurancePrice: 20m);
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    // ── CreateBookingAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateBooking_RejectsStartDateInPast()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateBookingAsync(
            _renter.Id, new CreateBookingRequest(_tool.Id, DateTime.UtcNow.Date.AddDays(-1), End)));
    }

    [Fact]
    public async Task CreateBooking_RejectsEndDateNotAfterStart()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateBookingAsync(
            _renter.Id, new CreateBookingRequest(_tool.Id, Start, Start)));
    }

    [Fact]
    public async Task CreateBooking_RejectsRentingOwnTool()
    {
        await _db.SeedWalletAsync(_owner.Id, balance: 1000m);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateBookingAsync(
            _owner.Id, new CreateBookingRequest(_tool.Id, Start, End)));
    }

    [Fact]
    public async Task CreateBooking_RejectsOverlappingDates()
    {
        await _db.SeedWalletAsync(_renter.Id, balance: 1000m);
        await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Accepted,
            startDate: Start, endDate: End);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateBookingAsync(
            _renter.Id, new CreateBookingRequest(_tool.Id, Start.AddDays(1), End.AddDays(1))));
    }

    [Fact]
    public async Task CreateBooking_AllowsDatesOverlappingOnlyCancelledBookings()
    {
        await _db.SeedWalletAsync(_renter.Id, balance: 1000m);
        await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Cancelled,
            startDate: Start, endDate: End);

        var result = await _sut.CreateBookingAsync(
            _renter.Id, new CreateBookingRequest(_tool.Id, Start, End));

        Assert.True(result.BookingId > 0);
    }

    [Fact]
    public async Task CreateBooking_RejectsWhenWalletMissing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateBookingAsync(
            _renter.Id, new CreateBookingRequest(_tool.Id, Start, End)));
    }

    [Fact]
    public async Task CreateBooking_RejectsWhenBalanceInsufficient()
    {
        await _db.SeedWalletAsync(_renter.Id, balance: 49m); // needs 50

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateBookingAsync(
            _renter.Id, new CreateBookingRequest(_tool.Id, Start, End)));
    }

    [Fact]
    public async Task CreateBooking_ComputesCostsHoldsEscrowAndNotifiesOwner()
    {
        await _db.SeedWalletAsync(_renter.Id, balance: 80m);

        var result = await _sut.CreateBookingAsync(
            _renter.Id, new CreateBookingRequest(_tool.Id, Start, End));

        Assert.Equal(30m, result.RentalCost);           // 3 days × 10
        Assert.Equal(20m, result.InsuranceAmount);
        Assert.Equal(3m, result.PlatformFee);           // default 10% of 30
        Assert.Equal(50m, result.TotalCharged);         // rental + insurance

        var wallet = _db.Context.Wallets.Single(w => w.UserId == _renter.Id);
        Assert.Equal(30m, wallet.Balance);              // 80 - 50
        Assert.Equal(50m, wallet.HeldBalance);

        var tx = Assert.Single(_db.Context.WalletTransactions.Where(t => t.WalletId == wallet.Id).ToList());
        Assert.Equal(-50m, tx.Amount);
        Assert.Equal(WalletTransactionType.BookingDebit, tx.Type);

        var booking = _db.Context.Bookings.Single(b => b.Id == result.BookingId);
        Assert.Equal(BookingStatus.Pending, booking.Status);

        await _notifications.Received(1).CreateAsync(Arg.Is<CreateNotificationDto>(n =>
            n.UserId == _owner.Id && n.Type == NotificationType.Booking));
    }

    [Fact]
    public async Task CreateBooking_UsesPlatformFeePercentFromSystemSettings()
    {
        _db.Context.SystemSettings.Add(new SystemSetting { Key = "PlatformFeePercent", Value = "20" });
        await _db.Context.SaveChangesAsync();
        await _db.SeedWalletAsync(_renter.Id, balance: 100m);

        var result = await _sut.CreateBookingAsync(
            _renter.Id, new CreateBookingRequest(_tool.Id, Start, End));

        Assert.Equal(6m, result.PlatformFee); // 20% of 30
    }

    // ── Accept / Reject / Cancel ────────────────────────────────────────────

    [Fact]
    public async Task AcceptBooking_OnlyOwnerCanAccept()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Pending);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.AcceptBookingAsync(booking.Id, _renter.Id));
    }

    [Fact]
    public async Task AcceptBooking_OnlyPendingCanBeAccepted()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Active);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AcceptBookingAsync(booking.Id, _owner.Id));
    }

    [Fact]
    public async Task AcceptBooking_SetsAcceptedAndNotifiesRenter()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Pending);

        await _sut.AcceptBookingAsync(booking.Id, _owner.Id);

        Assert.Equal(BookingStatus.Accepted, _db.Context.Bookings.Single(b => b.Id == booking.Id).Status);
        await _notifications.Received(1).CreateAsync(Arg.Is<CreateNotificationDto>(n => n.UserId == _renter.Id));
    }

    [Fact]
    public async Task RejectBooking_RefundsRenterInFull()
    {
        await _db.SeedWalletAsync(_renter.Id, balance: 0m, heldBalance: 50m);
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Pending);

        await _sut.RejectBookingAsync(booking.Id, _owner.Id);

        var wallet = _db.Context.Wallets.Single(w => w.UserId == _renter.Id);
        Assert.Equal(50m, wallet.Balance);
        Assert.Equal(0m, wallet.HeldBalance);

        var stored = _db.Context.Bookings.Single(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Rejected, stored.Status);
        Assert.NotNull(stored.CompletedAt);

        var tx = Assert.Single(_db.Context.WalletTransactions.Where(t => t.WalletId == wallet.Id).ToList());
        Assert.Equal(WalletTransactionType.BookingRefund, tx.Type);
        Assert.Equal(50m, tx.Amount);
    }

    [Fact]
    public async Task CancelBooking_RejectedForUnrelatedUser()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Pending);
        var stranger = await _db.SeedUserAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CancelBookingAsync(booking.Id, stranger.Id));
    }

    [Fact]
    public async Task CancelBooking_ByRenterRefundsAndNotifiesOwner()
    {
        await _db.SeedWalletAsync(_renter.Id, balance: 0m, heldBalance: 50m);
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Accepted);

        await _sut.CancelBookingAsync(booking.Id, _renter.Id);

        Assert.Equal(BookingStatus.Cancelled, _db.Context.Bookings.Single(b => b.Id == booking.Id).Status);
        Assert.Equal(50m, _db.Context.Wallets.Single(w => w.UserId == _renter.Id).Balance);
        await _notifications.Received(1).CreateAsync(Arg.Is<CreateNotificationDto>(n => n.UserId == _owner.Id));
    }

    [Fact]
    public async Task CancelBooking_RejectedWhenAlreadyActive()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Active);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CancelBookingAsync(booking.Id, _renter.Id));
    }

    // ── Handover flow ───────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitDeliveryHandover_MovesBookingToDeliveryHandover()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Accepted);

        await _sut.SubmitDeliveryHandoverAsync(booking.Id, _owner.Id, new SubmitHandoverRequest("delivered", null));

        Assert.Equal(BookingStatus.DeliveryHandover, _db.Context.Bookings.Single(b => b.Id == booking.Id).Status);
        var handover = Assert.Single(_db.Context.Handovers.Where(h => h.BookingId == booking.Id).ToList());
        Assert.Equal(HandoverType.Delivery, handover.Type);
        await _notifications.Received(1).CreateAsync(Arg.Is<CreateNotificationDto>(n =>
            n.UserId == _renter.Id && n.Type == NotificationType.HandoverPending));
    }

    [Fact]
    public async Task RespondDeliveryHandover_AcceptActivatesBooking()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Accepted);
        await _sut.SubmitDeliveryHandoverAsync(booking.Id, _owner.Id, new SubmitHandoverRequest(null, null));

        await _sut.RespondDeliveryHandoverAsync(booking.Id, _renter.Id, new RespondHandoverRequest(true, "all good"));

        Assert.Equal(BookingStatus.Active, _db.Context.Bookings.Single(b => b.Id == booking.Id).Status);
        var handover = _db.Context.Handovers.Single(h => h.BookingId == booking.Id);
        Assert.True(handover.IsAccepted);
    }

    [Fact]
    public async Task RespondDeliveryHandover_RejectDisputesBookingAndOpensIssue()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Accepted);
        await _sut.SubmitDeliveryHandoverAsync(booking.Id, _owner.Id, new SubmitHandoverRequest(null, null));

        await _sut.RespondDeliveryHandoverAsync(booking.Id, _renter.Id, new RespondHandoverRequest(false, "damaged"));

        Assert.Equal(BookingStatus.Disputed, _db.Context.Bookings.Single(b => b.Id == booking.Id).Status);
        var issue = Assert.Single(_db.Context.IssueReports.Where(i => i.BookingId == booking.Id).ToList());
        Assert.Equal(_renter.Id, issue.ReporterId);
        await _notifications.Received(1).CreateAsync(Arg.Is<CreateNotificationDto>(n =>
            n.UserId == _owner.Id && n.Type == NotificationType.HandoverDisputed));
    }

    [Fact]
    public async Task RespondReturnHandover_OnTimeReturn_SettlesEscrowCorrectly()
    {
        // rental 30, insurance 20, fee 3 → owner nets 27, renter gets insurance 20 back
        await _db.SeedWalletAsync(_renter.Id, balance: 0m, heldBalance: 50m);
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Active,
            startDate: DateTime.UtcNow.Date.AddDays(-5), endDate: DateTime.UtcNow.Date.AddDays(2));

        await _sut.SubmitReturnHandoverAsync(booking.Id, _renter.Id, new SubmitHandoverRequest("returned", null));
        await _sut.RespondReturnHandoverAsync(booking.Id, _owner.Id, new RespondHandoverRequest(true, null));

        var stored = _db.Context.Bookings.Single(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Completed, stored.Status);
        Assert.NotNull(stored.CompletedAt);

        var ownerWallet = _db.Context.Wallets.Single(w => w.UserId == _owner.Id);
        Assert.Equal(27m, ownerWallet.Balance); // 30 - 3 fee

        var renterWallet = _db.Context.Wallets.Single(w => w.UserId == _renter.Id);
        Assert.Equal(20m, renterWallet.Balance);   // insurance refund
        Assert.Equal(0m, renterWallet.HeldBalance);

        var ledger = Assert.Single(_db.Context.PlatformRevenueLedgers.Where(l => l.BookingId == booking.Id).ToList());
        Assert.Equal(3m, ledger.Amount);

        var types = _db.Context.WalletTransactions.Select(t => t.Type).ToList();
        Assert.Contains(WalletTransactionType.EarningCredit, types);
        Assert.Contains(WalletTransactionType.InsuranceRefund, types);
        Assert.DoesNotContain(WalletTransactionType.LateFeeCredit, types);
    }

    [Fact]
    public async Task RespondReturnHandover_LateReturn_ChargesLateFeeCappedByInsurance()
    {
        await _db.SeedWalletAsync(_renter.Id, balance: 0m, heldBalance: 50m);
        // Booking ended ~48h ago → late fee = ceil(48+) hours × (10/24) ≈ > 20 → capped at insurance (20)
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Active,
            startDate: DateTime.UtcNow.AddDays(-5), endDate: DateTime.UtcNow.AddDays(-2));

        await _sut.SubmitReturnHandoverAsync(booking.Id, _renter.Id, new SubmitHandoverRequest(null, null));
        await _sut.RespondReturnHandoverAsync(booking.Id, _owner.Id, new RespondHandoverRequest(true, null));

        var ownerWallet = _db.Context.Wallets.Single(w => w.UserId == _owner.Id);
        Assert.Equal(47m, ownerWallet.Balance); // 27 earnings + 20 late fee (capped)

        var renterWallet = _db.Context.Wallets.Single(w => w.UserId == _renter.Id);
        Assert.Equal(0m, renterWallet.Balance); // whole insurance consumed by late fee
        Assert.Equal(0m, renterWallet.HeldBalance);

        var types = _db.Context.WalletTransactions.Select(t => t.Type).ToList();
        Assert.Contains(WalletTransactionType.LateFeeCredit, types);
        Assert.DoesNotContain(WalletTransactionType.InsuranceRefund, types);
    }

    [Fact]
    public async Task RespondReturnHandover_RejectDisputesBooking()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Active);
        await _sut.SubmitReturnHandoverAsync(booking.Id, _renter.Id, new SubmitHandoverRequest(null, null));

        await _sut.RespondReturnHandoverAsync(booking.Id, _owner.Id, new RespondHandoverRequest(false, "broken"));

        Assert.Equal(BookingStatus.Disputed, _db.Context.Bookings.Single(b => b.Id == booking.Id).Status);
        Assert.Single(_db.Context.IssueReports.Where(i => i.BookingId == booking.Id).ToList());
    }

    // ── ReportIssue / queries ───────────────────────────────────────────────

    [Fact]
    public async Task ReportIssue_MovesBookingToDisputedAndNotifiesOtherParty()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Active);

        await _sut.ReportIssueAsync(booking.Id, _renter.Id, new ReportIssueRequest("Broken", "It broke"));

        Assert.Equal(BookingStatus.Disputed, _db.Context.Bookings.Single(b => b.Id == booking.Id).Status);
        await _notifications.Received(1).CreateAsync(Arg.Is<CreateNotificationDto>(n =>
            n.UserId == _owner.Id && n.Type == NotificationType.IssueReport));
    }

    [Fact]
    public async Task ReportIssue_RejectedOnPendingBooking()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Pending);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ReportIssueAsync(booking.Id, _renter.Id, new ReportIssueRequest("t", "d")));
    }

    [Fact]
    public async Task GetBookingById_OnlyPartiesCanView()
    {
        var booking = await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Pending);
        var stranger = await _db.SeedUserAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetBookingByIdAsync(booking.Id, stranger.Id));
    }

    [Fact]
    public async Task GetMyBookings_ComputesRenterAllowedActions()
    {
        await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Pending);

        var card = Assert.Single(await _sut.GetMyBookingsAsync(_renter.Id));

        Assert.Equal(3, card.Days);
        Assert.Contains("Cancel", card.AllowedActions);
        Assert.Contains("MessageOwner", card.AllowedActions);
        Assert.DoesNotContain("Accept", card.AllowedActions);
    }

    [Fact]
    public async Task GetReceivedBookings_ComputesOwnerAllowedActions()
    {
        await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Pending);

        var card = Assert.Single(await _sut.GetReceivedBookingsAsync(_owner.Id));

        Assert.Contains("Accept", card.AllowedActions);
        Assert.Contains("Reject", card.AllowedActions);
    }

    [Fact]
    public async Task GetToolAvailability_ReturnsActiveBookedRangesForMonth()
    {
        var start = DateTime.UtcNow.Date.AddDays(3);
        await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Accepted,
            startDate: start, endDate: start.AddDays(2));
        await _db.SeedBookingAsync(_tool.Id, _renter.Id, BookingStatus.Cancelled,
            startDate: start, endDate: start.AddDays(2));

        var result = await _sut.GetToolAvailabilityAsync(_tool.Id, start.Year, start.Month);

        Assert.Single(result.BookedRanges);
    }
}
