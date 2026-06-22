using Ehgiz.Application.DTOs.Admin;
using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.DTOs.Handovers;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Settings;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace Ehgiz.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _uow;
    private readonly string _handoverUploadPath;

    public AdminService(IUnitOfWork uow, IWebHostEnvironment env)
    {
        _uow = uow;
        _handoverUploadPath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "handover");
    }

    // ── List Disputed Bookings ──────────────────────────────────────────────
    public async Task<IEnumerable<BookingDto>> GetDisputedBookingsAsync()
    {
        var disputed = await _uow.Bookings.GetDisputedBookingsAsync();
        return disputed.Select(MapToDto);
    }

    // ── Get Dispute Details ─────────────────────────────────────────────────
    public async Task<DisputeDetailsDto> GetDisputeDetailsAsync(int bookingId)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
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
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        await using var transaction = await _uow.BeginTransactionAsync();

        var ownerWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.Tool.OwnerId);
        var renterWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.RenterId);

        // Owner gets full escrow (rental + insurance) — covers damage/non-return cases
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
        await CleanupHandoverImagesAsync(bookingId);
        await _uow.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    // ── 2. Resolve in Favor of Renter ───────────────────────────────────────
    public async Task ResolveInFavorOfRenterAsync(int bookingId, ResolveDisputeRequest dto)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        await using var transaction = await _uow.BeginTransactionAsync();

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
        await CleanupHandoverImagesAsync(bookingId);
        await _uow.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    // ── 3. Partial Refund ───────────────────────────────────────────────────
    public async Task ResolvePartialRefundAsync(int bookingId, PartialRefundRequest dto)
    {
        if (dto.RefundPercentage < 1 || dto.RefundPercentage > 99)
            throw new InvalidOperationException("Refund percentage must be between 1 and 99.");

        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        await using var transaction = await _uow.BeginTransactionAsync();

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
        await CleanupHandoverImagesAsync(bookingId);
        await _uow.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    // ── 4. Force Complete ───────────────────────────────────────────────────
    public async Task ForceCompleteAsync(int bookingId, ResolveDisputeRequest dto)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        await using var transaction = await _uow.BeginTransactionAsync();

        // Normal settlement logic
        var ownerEarning = booking.RentalCost - booking.PlatformFee;
        var insuranceAmount = booking.InsuranceAmount;

        // Check for late return (from the return handover submission time)
        var lateFee = 0m;
        var returnHandover = booking.Handovers
            .FirstOrDefault(h => h.Type == HandoverType.Return);

        if (returnHandover != null && returnHandover.SubmittedAt > booking.EndDate)
        {
            var lateHours = Math.Ceiling((decimal)(returnHandover.SubmittedAt - booking.EndDate).TotalHours);
            var hourlyRate = booking.PricePerDay / 24m;
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

        // Record platform fee in ledger
        if (booking.PlatformFee > 0)
        {
            await _uow.PlatformRevenueLedgers.AddAsync(new PlatformRevenueLedger
            {
                BookingId = booking.Id,
                Amount = booking.PlatformFee,
                CreatedAt = DateTime.UtcNow
            });
        }

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

        FinalizeDispute(booking, BookingStatus.Completed, dto.ResolutionNotes);
        await CleanupHandoverImagesAsync(bookingId);
        await _uow.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    // ── 5. Force Cancel ─────────────────────────────────────────────────────
    public async Task ForceCancelAsync(int bookingId, ResolveDisputeRequest dto)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        await using var transaction = await _uow.BeginTransactionAsync();

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
        await CleanupHandoverImagesAsync(bookingId);
        await _uow.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    // ── Issue Report Management ─────────────────────────────────────────────

    public async Task<IEnumerable<IssueReportDto>> GetIssueReportsAsync()
    {
        var all = await _uow.IssueReports.GetAllAsync();
        return all
            .OrderByDescending(ir => ir.CreatedAt)
            .Select(ir => new IssueReportDto(
                Id: ir.Id,
                ReporterName: ir.Reporter?.FullName ?? string.Empty,
                Title: ir.Title,
                Description: ir.Description,
                Status: ir.Status?.ToString() ?? string.Empty,
                CreatedAt: ir.CreatedAt));
    }

    public async Task<IssueReportDto> GetIssueReportByIdAsync(int id)
    {
        var ir = await _uow.IssueReports.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Issue report {id} not found.");

        return new IssueReportDto(
            Id: ir.Id,
            ReporterName: ir.Reporter?.FullName ?? string.Empty,
            Title: ir.Title,
            Description: ir.Description,
            Status: ir.Status?.ToString() ?? string.Empty,
            CreatedAt: ir.CreatedAt);
    }

    public async Task UpdateIssueReportStatusAsync(int id, UpdateIssueStatusRequest dto)
    {
        var ir = await _uow.IssueReports.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Issue report {id} not found.");

        if (!Enum.TryParse<IssueReportStatus>(dto.Status, ignoreCase: true, out var newStatus))
            throw new InvalidOperationException(
                $"Invalid status '{dto.Status}'. Valid values: {string.Join(", ", Enum.GetNames<IssueReportStatus>())}");

        ir.Status = newStatus;
        await _uow.SaveChangesAsync();
    }

    // ── Platform Settings ───────────────────────────────────────────────────

    public async Task<decimal> GetPlatformFeeAsync()
    {
        var setting = await _uow.SystemSettings.GetByIdAsync("PlatformFeePercent");
        if (setting != null && decimal.TryParse(setting.Value, out var fee))
        {
            return fee;
        }
        return 10m; // Default
    }

    public async Task UpdatePlatformFeeAsync(decimal feePercent)
    {
        if (feePercent < 0 || feePercent > 100)
            throw new InvalidOperationException("Fee percentage must be between 0 and 100.");

        var setting = await _uow.SystemSettings.GetByIdAsync("PlatformFeePercent");
        if (setting == null)
        {
            setting = new SystemSetting { Key = "PlatformFeePercent", Value = feePercent.ToString() };
            await _uow.SystemSettings.AddAsync(setting);
        }
        else
        {
            setting.Value = feePercent.ToString();
        }

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

    private async Task CleanupHandoverImagesAsync(int bookingId)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId);
        if (booking == null) return;

        var images = booking.Handovers?.SelectMany(h => h.Images ?? new List<HandoverImage>()).ToList()
                     ?? new List<HandoverImage>();

        foreach (var image in images)
            _uow.HandoverImages.Remove(image);

        // Delete physical files
        var folderPath = Path.Combine(_handoverUploadPath, bookingId.ToString());
        if (Directory.Exists(folderPath))
            Directory.Delete(folderPath, recursive: true);
    }

    private static BookingDto MapToDto(Booking b)
    {
        var days = (int)(b.EndDate.Date - b.StartDate.Date).TotalDays;
        var rentalCost = b.RentalCost;

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
            InsurancePrice: b.InsuranceAmount,
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
