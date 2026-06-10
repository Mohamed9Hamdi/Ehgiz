using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;

namespace Ehgiz.Application.Services;

public class AdminPaymentService : IAdminPaymentService
{
    private readonly IUnitOfWork _uow;

    public AdminPaymentService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<BookingDto>> GetDisputedBookingsAsync()
    {
        var all = await _uow.Bookings.GetAllAsync();
        return all
            .Where(b => b.Payment != null && b.Payment.EscrowStatus == EscrowStatus.Held
                        && b.IssueReports.Any())
            .Select(MapToDto)
            .OrderByDescending(b => b.CreatedAt);
    }

    public async Task ResolveDisputeInFavorOfOwnerAsync(int bookingId)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateBookingForDispute(booking);

        var ownerWallet = await _uow.Wallets.GetByUserIdAsync(booking.Tool.OwnerId)
            ?? throw new InvalidOperationException("Owner wallet not found.");

        var renterWallet = await _uow.Wallets.GetByUserIdAsync(booking.RenterId)
            ?? throw new InvalidOperationException("Renter wallet not found.");

        // Release full escrow to owner
        ownerWallet.Balance += booking.TotalPrice;
        ownerWallet.UpdatedAt = DateTime.UtcNow;

        renterWallet.HeldBalance -= booking.TotalPrice;
        renterWallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = ownerWallet.Id,
            Amount = booking.TotalPrice,
            Type = WalletTransactionType.EarningCredit,
            Reference = bookingId.ToString(),
            Description = $"Dispute resolved in owner's favor — booking #{bookingId}",
            CreatedAt = DateTime.UtcNow
        });

        booking.Payment!.EscrowStatus = EscrowStatus.Released;

        foreach (var issue in booking.IssueReports)
            issue.Status = IssueReportStatus.Resolved;

        await _uow.SaveChangesAsync();
    }

    public async Task ResolveDisputeInFavorOfRenterAsync(int bookingId)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateBookingForDispute(booking);

        var renterWallet = await _uow.Wallets.GetByUserIdAsync(booking.RenterId)
            ?? throw new InvalidOperationException("Renter wallet not found.");

        // Refund full amount to renter
        renterWallet.Balance += booking.TotalPrice;
        renterWallet.HeldBalance -= booking.TotalPrice;
        renterWallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = renterWallet.Id,
            Amount = booking.TotalPrice,
            Type = WalletTransactionType.BookingRefund,
            Reference = bookingId.ToString(),
            Description = $"Dispute resolved in renter's favor — booking #{bookingId}",
            CreatedAt = DateTime.UtcNow
        });

        booking.Payment!.EscrowStatus = EscrowStatus.Refunded;
        booking.Payment.PaymentStatus = PaymentStatus.Refunded;

        foreach (var issue in booking.IssueReports)
            issue.Status = IssueReportStatus.Resolved;

        await _uow.SaveChangesAsync();
    }

    // ── Helper ──────────────────────────────────────────────────────────────
    private static void ValidateBookingForDispute(Booking booking)
    {
        if (booking.Payment is null)
            throw new InvalidOperationException("No payment record associated with this booking.");

        if (booking.Payment.EscrowStatus != EscrowStatus.Held)
            throw new InvalidOperationException("Escrow is not in 'Held' state — cannot resolve dispute.");
    }

    private static BookingDto MapToDto(Booking b)
    {
        var days = (int)(b.EndDate.Date - b.StartDate.Date).TotalDays;
        var rentalCost = b.Tool != null ? days * b.Tool.PricePerDay : 0;

        return new BookingDto(
            Id: b.Id,
            ToolId: b.ToolId,
            ToolName: b.Tool?.Name ?? string.Empty,
            OwnerName: b.Tool?.Owner?.FullName ?? string.Empty,
            RenterName: b.Renter?.FullName ?? string.Empty,
            StartDate: b.StartDate,
            EndDate: b.EndDate,
            Days: days,
            RentalCost: rentalCost,
            InsurancePrice: b.Tool?.InsurancePrice ?? 0,
            TotalPrice: b.TotalPrice,
            Status: b.Status?.ToString() ?? string.Empty,
            PaymentStatus: b.Payment?.PaymentStatus?.ToString(),
            EscrowStatus: b.Payment?.EscrowStatus?.ToString(),
            CreatedAt: b.CreatedAt);
    }
}
