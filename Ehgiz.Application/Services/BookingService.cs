using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Settings;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Microsoft.Extensions.Options;

namespace Ehgiz.Application.Services;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _uow;
    private readonly PlatformSettings _platform;

    public BookingService(IUnitOfWork uow, IOptions<PlatformSettings> platform)
    {
        _uow = uow;
        _platform = platform.Value;
    }

    public async Task<CreateBookingResponse> CreateBookingAsync(int renterId, CreateBookingRequest dto)
    {
        // --- Validate dates ---
        if (dto.StartDate.Date < DateTime.UtcNow.Date)
            throw new InvalidOperationException("Start date cannot be in the past.");

        if (dto.EndDate <= dto.StartDate)
            throw new InvalidOperationException("End date must be after start date.");

        // --- Load tool ---
        var tool = await _uow.Tools.GetByIdAsync(dto.ToolId)
            ?? throw new KeyNotFoundException($"Tool {dto.ToolId} not found.");

        if (!tool.IsAvailable)
            throw new InvalidOperationException("This tool is not available for booking.");

        if (tool.OwnerId == renterId)
            throw new InvalidOperationException("You cannot rent your own tool.");

        // --- Check overlapping bookings ---
        var hasConflict = await _uow.Bookings
            .HasOverlappingBookingAsync(dto.ToolId, dto.StartDate, dto.EndDate);

        if (hasConflict)
            throw new InvalidOperationException("The tool is already booked for the selected dates.");

        // --- Calculate costs ---
        var days = (int)(dto.EndDate.Date - dto.StartDate.Date).TotalDays;
        var rentalCost = days * tool.PricePerDay;
        var platformFee = Math.Round(rentalCost * (_platform.FeePercent / 100m), 2);
        var totalCharged = rentalCost + tool.InsurancePrice;

        // --- Check renter wallet ---
        var wallet = await _uow.Wallets.GetByUserIdAsync(renterId)
            ?? throw new InvalidOperationException("Wallet not found. Please contact support.");

        if (wallet.Balance < totalCharged)
            throw new InvalidOperationException(
                $"Insufficient wallet balance. Required: {totalCharged:C}, Available: {wallet.Balance:C}");

        // --- Deduct from wallet ---
        wallet.Balance -= totalCharged;
        wallet.HeldBalance += totalCharged;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = -totalCharged,
            Type = WalletTransactionType.BookingDebit,
            Reference = dto.ToolId.ToString(),
            Description = $"Booking for '{tool.Name}' ({days} days) + insurance",
            CreatedAt = DateTime.UtcNow
        });

        // --- Create booking ---
        var booking = new Booking
        {
            ToolId = dto.ToolId,
            RenterId = renterId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TotalPrice = totalCharged,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Bookings.AddAsync(booking);
        await _uow.SaveChangesAsync();

        return new CreateBookingResponse(
            BookingId: booking.Id,
            RentalCost: rentalCost,
            InsuranceAmount: tool.InsurancePrice,
            PlatformFee: platformFee,
            TotalCharged: totalCharged,
            Currency: "usd");
    }

    public async Task<IEnumerable<BookingDto>> GetMyBookingsAsync(int renterId)
    {
        var all = await _uow.Bookings.GetAllAsync();
        return all
            .Where(b => b.RenterId == renterId)
            .Select(MapToDto)
            .OrderByDescending(b => b.CreatedAt);
    }

    public async Task<IEnumerable<BookingDto>> GetReceivedBookingsAsync(int ownerId)
    {
        var all = await _uow.Bookings.GetAllAsync();
        return all
            .Where(b => b.Tool.OwnerId == ownerId)
            .Select(MapToDto)
            .OrderByDescending(b => b.CreatedAt);
    }

    public async Task<BookingDto> GetBookingByIdAsync(int bookingId, int requestingUserId)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.RenterId != requestingUserId && booking.Tool.OwnerId != requestingUserId)
            throw new UnauthorizedAccessException("You are not authorized to view this booking.");

        return MapToDto(booking);
    }

    public async Task CancelBookingAsync(int bookingId, int requestingUserId)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.RenterId != requestingUserId && booking.Tool.OwnerId != requestingUserId)
            throw new UnauthorizedAccessException("You are not authorized to cancel this booking.");

        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException("Only pending bookings can be cancelled.");

        // --- Full refund to renter wallet ---
        var renterWallet = await _uow.Wallets.GetByUserIdAsync(booking.RenterId)
            ?? throw new InvalidOperationException("Renter wallet not found.");

        renterWallet.Balance += booking.TotalPrice;
        renterWallet.HeldBalance -= booking.TotalPrice;
        renterWallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = renterWallet.Id,
            Amount = booking.TotalPrice,
            Type = WalletTransactionType.BookingRefund,
            Reference = bookingId.ToString(),
            Description = $"Refund for cancelled booking #{bookingId}",
            CreatedAt = DateTime.UtcNow
        });

        booking.Status = BookingStatus.Cancelled;

        if (booking.Payment != null)
        {
            booking.Payment.PaymentStatus = PaymentStatus.Refunded;
            booking.Payment.EscrowStatus = EscrowStatus.Refunded;
        }

        await _uow.SaveChangesAsync();
    }

    public async Task CompleteBookingAsync(int bookingId, int requestingUserId)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.Tool.OwnerId != requestingUserId)
            throw new UnauthorizedAccessException("Only the tool owner can mark a booking as complete.");

        if (booking.Status != BookingStatus.Active && booking.Status != BookingStatus.Accepted)
            throw new InvalidOperationException("Booking must be active to be completed.");

        // --- Calculate money split ---
        var days = (int)(booking.EndDate.Date - booking.StartDate.Date).TotalDays;
        var rentalCost = days * booking.Tool.PricePerDay;
        var platformFee = Math.Round(rentalCost * (_platform.FeePercent / 100m), 2);
        var ownerEarning = rentalCost - platformFee;
        var insuranceAmount = booking.Tool.InsurancePrice;

        // --- Credit owner wallet ---
        var ownerWallet = await _uow.Wallets.GetByUserIdAsync(booking.Tool.OwnerId)
            ?? throw new InvalidOperationException("Owner wallet not found.");

        ownerWallet.Balance += ownerEarning;
        ownerWallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = ownerWallet.Id,
            Amount = ownerEarning,
            Type = WalletTransactionType.EarningCredit,
            Reference = bookingId.ToString(),
            Description = $"Earnings for booking #{bookingId} (after 10% platform fee)",
            CreatedAt = DateTime.UtcNow
        });

        // --- Refund insurance to renter wallet ---
        var renterWallet = await _uow.Wallets.GetByUserIdAsync(booking.RenterId)
            ?? throw new InvalidOperationException("Renter wallet not found.");

        renterWallet.Balance += insuranceAmount;
        renterWallet.HeldBalance -= booking.TotalPrice;
        renterWallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = renterWallet.Id,
            Amount = insuranceAmount,
            Type = WalletTransactionType.InsuranceRefund,
            Reference = bookingId.ToString(),
            Description = $"Insurance refund for completed booking #{bookingId}",
            CreatedAt = DateTime.UtcNow
        });

        // --- Update booking & payment status ---
        booking.Status = BookingStatus.Completed;
        booking.Tool.IsAvailable = true;

        if (booking.Payment != null)
        {
            booking.Payment.EscrowStatus = EscrowStatus.Released;
        }

        await _uow.SaveChangesAsync();
    }

    // ── Helper ──────────────────────────────────────────────────────────────
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

