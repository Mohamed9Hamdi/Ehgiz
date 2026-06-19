using Ehgiz.Application.DTOs.Admin;
using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.DTOs.Handovers;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Settings;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Microsoft.Extensions.Options;

namespace Ehgiz.Application.Services;

public class AdminPaymentService : IAdminPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly PlatformSettings _platform;

    public AdminPaymentService(IUnitOfWork uow, IOptions<PlatformSettings> platform)
    {
        _uow = uow;
        _platform = platform.Value;
    }

    // ── List Disputed Bookings ──────────────────────────────────────────────
    public async Task<IEnumerable<BookingDto>> GetDisputedBookingsAsync()
    {
        var all = await _uow.Bookings.GetAllAsync();
        return all
            .Where(b => b.Status == BookingStatus.Disputed)
            .Select(MapToDto)
            .OrderByDescending(b => b.CreatedAt);
    }

    // ── Get Dispute Details ─────────────────────────────────────────────────
    public async Task<DisputeDetailsDto> GetDisputeDetailsAsync(int bookingId)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.Status != BookingStatus.Disputed)
            throw new InvalidOperationException("This booking is not in a disputed state.");

        var bookingDto = MapToDto(booking);

        var issues = booking.IssueReports.Select(ir => new IssueReportDto(
            Id: ir.Id,
            ReporterName: ir.Reporter?.FullName ?? string.Empty,
            Title: ir.Title,
            Description: ir.Description,
            Status: ir.Status?.ToString() ?? string.Empty,
            CreatedAt: ir.CreatedAt));

        var handovers = booking.Handovers.Select(h => new HandoverDto(
            Id: h.Id,
            BookingId: h.BookingId,
            Type: h.Type.ToString(),
            SubmittedByName: h.SubmittedByUser?.FullName ?? string.Empty,
            SubmitterNotes: h.SubmitterNotes,
            SubmittedAt: h.SubmittedAt,
            RespondedByName: h.RespondedByUser?.FullName,
            ResponderNotes: h.ResponderNotes,
            IsAccepted: h.IsAccepted,
            RespondedAt: h.RespondedAt,
            Images: h.Images?.Select(i => new HandoverImageDto(i.Id, i.ImageUrl, i.Caption))));

        return new DisputeDetailsDto(bookingDto, issues, handovers);
    }

    // ── 1. Resolve in Favor of Owner ────────────────────────────────────────
    public async Task ResolveInFavorOfOwnerAsync(int bookingId, ResolveDisputeRequest dto)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        var ownerWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.Tool.OwnerId);

        var renterWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.RenterId);

        // Owner gets full escrow (rental + insurance)
        ownerWallet.Balance += booking.TotalPrice;
        ownerWallet.UpdatedAt = DateTime.UtcNow;

        renterWallet.HeldBalance -= booking.TotalPrice;
        renterWallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = ownerWallet.Id,
            Amount = booking.TotalPrice,
            Type = WalletTransactionType.DisputeCredit,
            Reference = bookingId.ToString(),
            Description = $"Dispute resolved in owner's favor — booking #{bookingId}",
            CreatedAt = DateTime.UtcNow
        });

        FinalizeDispute(booking, BookingStatus.Completed, dto.ResolutionNotes);
        await NotifyBothPartiesAsync(booking, "resolved in favor of the owner");
        await CleanupHandoverImagesAsync(bookingId);
        await _uow.SaveChangesAsync();
    }

    // ── 2. Resolve in Favor of Renter ───────────────────────────────────────
    public async Task ResolveInFavorOfRenterAsync(int bookingId, ResolveDisputeRequest dto)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        var renterWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.RenterId);

        // Full refund to renter
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

        if (booking.Payment != null)
        {
            booking.Payment.EscrowStatus = EscrowStatus.Refunded;
            booking.Payment.PaymentStatus = PaymentStatus.Refunded;
        }

        FinalizeDispute(booking, BookingStatus.Cancelled, dto.ResolutionNotes);
        await NotifyBothPartiesAsync(booking, "resolved in favor of the renter");
        await CleanupHandoverImagesAsync(bookingId);
        await _uow.SaveChangesAsync();
    }

    // ── 3. Partial Refund ───────────────────────────────────────────────────
    public async Task ResolvePartialRefundAsync(int bookingId, PartialRefundRequest dto)
    {
        if (dto.RefundPercentage < 1 || dto.RefundPercentage > 99)
            throw new InvalidOperationException("Refund percentage must be between 1 and 99.");

        var booking = await _uow.Bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        var refundAmount = Math.Round(booking.TotalPrice * (dto.RefundPercentage / 100m), 2);
        var ownerAmount = booking.TotalPrice - refundAmount;

        var renterWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.RenterId);

        var ownerWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.Tool.OwnerId);

        // Partial refund to renter
        renterWallet.Balance += refundAmount;
        renterWallet.HeldBalance -= booking.TotalPrice;
        renterWallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = renterWallet.Id,
            Amount = refundAmount,
            Type = WalletTransactionType.PartialRefund,
            Reference = bookingId.ToString(),
            Description = $"Partial refund ({dto.RefundPercentage}%) for dispute — booking #{bookingId}",
            CreatedAt = DateTime.UtcNow
        });

        // Remainder to owner
        ownerWallet.Balance += ownerAmount;
        ownerWallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = ownerWallet.Id,
            Amount = ownerAmount,
            Type = WalletTransactionType.DisputeCredit,
            Reference = bookingId.ToString(),
            Description = $"Dispute partial resolution — {100 - dto.RefundPercentage}% of escrow — booking #{bookingId}",
            CreatedAt = DateTime.UtcNow
        });

        if (booking.Payment != null)
        {
            booking.Payment.EscrowStatus = EscrowStatus.Released;
        }

        FinalizeDispute(booking, BookingStatus.Completed, dto.ResolutionNotes);
        await NotifyBothPartiesAsync(booking, $"resolved with a {dto.RefundPercentage}% partial refund to the renter");
        await CleanupHandoverImagesAsync(bookingId);
        await _uow.SaveChangesAsync();
    }

    // ── 4. Force Complete ───────────────────────────────────────────────────
    public async Task ForceCompleteAsync(int bookingId, ResolveDisputeRequest dto)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        // Normal settlement logic
        var days = (int)(booking.EndDate.Date - booking.StartDate.Date).TotalDays;
        var rentalCost = days * booking.Tool.PricePerDay;
        var platformFee = Math.Round(rentalCost * (_platform.FeePercent / 100m), 2);
        var ownerEarning = rentalCost - platformFee;
        var insuranceAmount = booking.Tool.InsurancePrice;

        // Check for late return (from the return handover submission time)
        var lateFee = 0m;
        var returnHandover = booking.Handovers
            .FirstOrDefault(h => h.Type == HandoverType.Return);

        if (returnHandover != null && returnHandover.SubmittedAt > booking.EndDate)
        {
            var lateHours = Math.Ceiling((decimal)(returnHandover.SubmittedAt - booking.EndDate).TotalHours);
            var hourlyRate = booking.Tool.PricePerDay / 24m;
            lateFee = Math.Min(lateHours * hourlyRate, insuranceAmount);
            lateFee = Math.Round(lateFee, 2);
        }

        var insuranceRefund = insuranceAmount - lateFee;

        // Credit owner
        var ownerWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.Tool.OwnerId);

        ownerWallet.Balance += ownerEarning + lateFee;
        ownerWallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = ownerWallet.Id,
            Amount = ownerEarning,
            Type = WalletTransactionType.EarningCredit,
            Reference = bookingId.ToString(),
            Description = $"Force-complete earnings for booking #{bookingId}",
            CreatedAt = DateTime.UtcNow
        });

        if (lateFee > 0)
        {
            await _uow.WalletTransactions.AddAsync(new WalletTransaction
            {
                WalletId = ownerWallet.Id,
                Amount = lateFee,
                Type = WalletTransactionType.LateFeeCredit,
                Reference = bookingId.ToString(),
                Description = $"Late return fee for booking #{bookingId}",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Refund renter insurance (minus late fee)
        var renterWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.RenterId);

        renterWallet.HeldBalance -= booking.TotalPrice;
        renterWallet.UpdatedAt = DateTime.UtcNow;

        if (insuranceRefund > 0)
        {
            renterWallet.Balance += insuranceRefund;

            await _uow.WalletTransactions.AddAsync(new WalletTransaction
            {
                WalletId = renterWallet.Id,
                Amount = insuranceRefund,
                Type = WalletTransactionType.InsuranceRefund,
                Reference = bookingId.ToString(),
                Description = lateFee > 0
                    ? $"Insurance refund for booking #{bookingId} (late fee of {lateFee:C} deducted)"
                    : $"Insurance refund for force-completed booking #{bookingId}",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (booking.Payment != null)
        {
            booking.Payment.EscrowStatus = EscrowStatus.Released;
        }

        booking.Tool.IsAvailable = true;

        FinalizeDispute(booking, BookingStatus.Completed, dto.ResolutionNotes);
        await NotifyBothPartiesAsync(booking, "force-completed by admin with normal settlement");
        await CleanupHandoverImagesAsync(bookingId);
        await _uow.SaveChangesAsync();
    }

    // ── 5. Force Cancel ─────────────────────────────────────────────────────
    public async Task ForceCancelAsync(int bookingId, ResolveDisputeRequest dto)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        // Full refund to renter
        var renterWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.RenterId);

        renterWallet.Balance += booking.TotalPrice;
        renterWallet.HeldBalance -= booking.TotalPrice;
        renterWallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = renterWallet.Id,
            Amount = booking.TotalPrice,
            Type = WalletTransactionType.BookingRefund,
            Reference = bookingId.ToString(),
            Description = $"Force-cancel refund for booking #{bookingId}",
            CreatedAt = DateTime.UtcNow
        });

        if (booking.Payment != null)
        {
            booking.Payment.EscrowStatus = EscrowStatus.Refunded;
            booking.Payment.PaymentStatus = PaymentStatus.Refunded;
        }

        FinalizeDispute(booking, BookingStatus.Cancelled, dto.ResolutionNotes);
        await NotifyBothPartiesAsync(booking, "force-cancelled by admin with full refund");
        await CleanupHandoverImagesAsync(bookingId);
        await _uow.SaveChangesAsync();
    }

    // ── Private Helpers ─────────────────────────────────────────────────────

    private static void ValidateDisputed(Booking booking)
    {
        if (booking.Status != BookingStatus.Disputed)
            throw new InvalidOperationException("This booking is not in a disputed state.");
    }

    private static void FinalizeDispute(Booking booking, BookingStatus finalStatus, string? resolutionNotes)
    {
        booking.Status = finalStatus;
        booking.CompletedAt = DateTime.UtcNow;
        booking.AdminResolutionNotes = resolutionNotes;

        foreach (var issue in booking.IssueReports)
            issue.Status = IssueReportStatus.Resolved;
    }

    private async Task NotifyBothPartiesAsync(Booking booking, string resolution)
    {
        var toolName = booking.Tool?.Name ?? "the tool";

        await _uow.Notifications.AddAsync(new Notification
        {
            UserId = booking.RenterId,
            Type = NotificationType.DisputeResolved,
            Content = $"Dispute for booking #{booking.Id} ('{toolName}') has been {resolution}.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _uow.Notifications.AddAsync(new Notification
        {
            UserId = booking.Tool!.OwnerId,
            Type = NotificationType.DisputeResolved,
            Content = $"Dispute for booking #{booking.Id} ('{toolName}') has been {resolution}.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task CleanupHandoverImagesAsync(int bookingId)
    {
        var allImages = await _uow.HandoverImages.GetAllAsync();
        var images = allImages
            .Where(hi => hi.Handover.BookingId == bookingId)
            .ToList();

        foreach (var image in images)
            _uow.HandoverImages.Remove(image);
    }

    private static BookingDto MapToDto(Booking b)
    {
        var days = (int)(b.EndDate.Date - b.StartDate.Date).TotalDays;
        var rentalCost = b.Tool != null ? days * b.Tool.PricePerDay : 0;

        return new BookingDto(
            Id: b.Id,
            ToolId: b.ToolId,
            ToolName: b.Tool?.Name ?? string.Empty,
            ToolImageUrl: b.Tool?.Images?.FirstOrDefault()?.ImageUrl,
            OwnerId: b.Tool?.OwnerId ?? 0,
            OwnerName: b.Tool?.Owner?.FullName ?? string.Empty,
            OwnerProfileImageUrl: b.Tool?.Owner?.ProfileImageUrl,
            RenterId: b.RenterId,
            RenterName: b.Renter?.FullName ?? string.Empty,
            RenterProfileImageUrl: b.Renter?.ProfileImageUrl,
            StartDate: b.StartDate,
            EndDate: b.EndDate,
            Days: days,
            RentalCost: rentalCost,
            InsurancePrice: b.Tool?.InsurancePrice ?? 0,
            TotalPrice: b.TotalPrice,
            Status: b.Status?.ToString() ?? string.Empty,
            PaymentStatus: b.Payment?.PaymentStatus?.ToString(),
            EscrowStatus: b.Payment?.EscrowStatus?.ToString(),
            CreatedAt: b.CreatedAt,
            AdminResolutionNotes: b.AdminResolutionNotes,
            Handovers: b.Handovers?.Select(h => new HandoverDto(
                Id: h.Id,
                BookingId: h.BookingId,
                Type: h.Type.ToString(),
                SubmittedByName: h.SubmittedByUser?.FullName ?? string.Empty,
                SubmitterNotes: h.SubmitterNotes,
                SubmittedAt: h.SubmittedAt,
                RespondedByName: h.RespondedByUser?.FullName,
                ResponderNotes: h.ResponderNotes,
                IsAccepted: h.IsAccepted,
                RespondedAt: h.RespondedAt,
                Images: h.Images?.Select(i => new HandoverImageDto(i.Id, i.ImageUrl, i.Caption))
            )),
            AllowedActions: [],
            HasReview: b.Reviews?.Any() ?? false);
    }
}
