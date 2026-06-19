using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.DTOs.Handovers;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Settings;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Ehgiz.Application.Services;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _uow;
    private readonly PlatformSettings _platform;
    private readonly string _handoverUploadPath;

    public BookingService(IUnitOfWork uow, IOptions<PlatformSettings> platform, IWebHostEnvironment env)
    {
        _uow = uow;
        _platform = platform.Value;
        _handoverUploadPath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "handover");
    }

    // ── Create Booking ──────────────────────────────────────────────────────
    public async Task<CreateBookingResponse> CreateBookingAsync(int renterId, CreateBookingRequest dto)
    {
        if (dto.StartDate.Date < DateTime.UtcNow.Date)
            throw new InvalidOperationException("Start date cannot be in the past.");

        if (dto.EndDate <= dto.StartDate)
            throw new InvalidOperationException("End date must be after start date.");

        var tool = await _uow.Tools.GetByIdAsync(dto.ToolId)
            ?? throw new KeyNotFoundException($"Tool {dto.ToolId} not found.");

        if (!tool.IsAvailable)
            throw new InvalidOperationException("This tool is not available for booking.");

        if (tool.OwnerId == renterId)
            throw new InvalidOperationException("You cannot rent your own tool.");

        var hasConflict = await _uow.Bookings
            .HasOverlappingBookingAsync(dto.ToolId, dto.StartDate, dto.EndDate);

        if (hasConflict)
            throw new InvalidOperationException("The tool is already booked for the selected dates.");

        var days = (int)(dto.EndDate.Date - dto.StartDate.Date).TotalDays;
        var rentalCost = days * tool.PricePerDay;
        var platformFee = Math.Round(rentalCost * (_platform.FeePercent / 100m), 2);
        var totalCharged = rentalCost + tool.InsurancePrice;

        var wallet = await _uow.Wallets.GetByUserIdAsync(renterId)
            ?? throw new InvalidOperationException("Wallet not found. Please top up your wallet first.");

        if (wallet.Balance < totalCharged)
            throw new InvalidOperationException(
                $"Insufficient wallet balance. Required: {totalCharged:C}, Available: {wallet.Balance:C}");

        // Deduct from wallet → hold in escrow
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

        // Notify owner about new booking request
        await _uow.Notifications.AddAsync(new Notification
        {
            UserId = tool.OwnerId,
            Type = NotificationType.BookingUpdate,
            Content = $"New booking request for '{tool.Name}' from {(await _uow.Users.GetByIdAsync(renterId))?.FullName ?? "a renter"}.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _uow.SaveChangesAsync();

        return new CreateBookingResponse(
            BookingId: booking.Id,
            RentalCost: rentalCost,
            InsuranceAmount: tool.InsurancePrice,
            PlatformFee: platformFee,
            TotalCharged: totalCharged,
            Currency: "usd");
    }

    // ── Accept Booking (Owner) ──────────────────────────────────────────────
    public async Task AcceptBookingAsync(int bookingId, int ownerId)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.Tool.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Only the tool owner can accept this booking.");

        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException("Only pending bookings can be accepted.");

        booking.Status = BookingStatus.Accepted;

        await _uow.Notifications.AddAsync(new Notification
        {
            UserId = booking.RenterId,
            Type = NotificationType.BookingUpdate,
            Content = $"Your booking for '{booking.Tool.Name}' has been accepted by the owner.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _uow.SaveChangesAsync();
    }

    // ── Reject Booking (Owner) ──────────────────────────────────────────────
    public async Task RejectBookingAsync(int bookingId, int ownerId)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.Tool.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Only the tool owner can reject this booking.");

        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException("Only pending bookings can be rejected.");

        // Full refund
        await RefundRenterAsync(booking);

        booking.Status = BookingStatus.Rejected;
        booking.CompletedAt = DateTime.UtcNow;

        if (booking.Payment != null)
        {
            booking.Payment.PaymentStatus = PaymentStatus.Refunded;
            booking.Payment.EscrowStatus = EscrowStatus.Refunded;
        }

        await _uow.Notifications.AddAsync(new Notification
        {
            UserId = booking.RenterId,
            Type = NotificationType.BookingUpdate,
            Content = $"Your booking for '{booking.Tool.Name}' has been rejected. A full refund has been issued.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _uow.SaveChangesAsync();
    }

    // ── Cancel Booking ──────────────────────────────────────────────────────
    public async Task CancelBookingAsync(int bookingId, int requestingUserId)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.RenterId != requestingUserId && booking.Tool.OwnerId != requestingUserId)
            throw new UnauthorizedAccessException("You are not authorized to cancel this booking.");

        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Accepted)
            throw new InvalidOperationException("Only pending or accepted bookings can be cancelled.");

        // Full refund
        await RefundRenterAsync(booking);

        booking.Status = BookingStatus.Cancelled;
        booking.CompletedAt = DateTime.UtcNow;

        if (booking.Payment != null)
        {
            booking.Payment.PaymentStatus = PaymentStatus.Refunded;
            booking.Payment.EscrowStatus = EscrowStatus.Refunded;
        }

        // Notify the other party
        var notifyUserId = booking.RenterId == requestingUserId
            ? booking.Tool.OwnerId
            : booking.RenterId;

        await _uow.Notifications.AddAsync(new Notification
        {
            UserId = notifyUserId,
            Type = NotificationType.BookingUpdate,
            Content = $"Booking #{bookingId} for '{booking.Tool.Name}' has been cancelled. Refund issued.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _uow.SaveChangesAsync();
    }

    // ── Submit Delivery Handover (Owner) ────────────────────────────────────
    public async Task SubmitDeliveryHandoverAsync(int bookingId, int ownerId, SubmitHandoverRequest dto)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.Tool.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Only the tool owner can submit a delivery handover.");

        if (booking.Status != BookingStatus.Accepted)
            throw new InvalidOperationException("Booking must be accepted before submitting delivery.");

        var handover = new Handover
        {
            BookingId = bookingId,
            Type = HandoverType.Delivery,
            SubmittedByUserId = ownerId,
            SubmitterNotes = dto.Notes,
            SubmittedAt = DateTime.UtcNow
        };

        await _uow.Handovers.AddAsync(handover);
        booking.Status = BookingStatus.DeliveryHandover;

        // Save handover images
        await SaveHandoverImagesAsync(handover, dto);

        await _uow.Notifications.AddAsync(new Notification
        {
            UserId = booking.RenterId,
            Type = NotificationType.HandoverPending,
            Content = $"The owner has submitted a delivery for '{booking.Tool.Name}'. Please inspect and confirm.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _uow.SaveChangesAsync();
    }

    // ── Respond to Delivery Handover (Renter) ───────────────────────────────
    public async Task RespondDeliveryHandoverAsync(int bookingId, int renterId, RespondHandoverRequest dto)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.RenterId != renterId)
            throw new UnauthorizedAccessException("Only the renter can respond to a delivery handover.");

        if (booking.Status != BookingStatus.DeliveryHandover)
            throw new InvalidOperationException("Booking is not in delivery handover state.");

        var handover = await _uow.Handovers.GetPendingHandoverAsync(bookingId, HandoverType.Delivery)
            ?? throw new InvalidOperationException("No pending delivery handover found.");

        handover.RespondedByUserId = renterId;
        handover.ResponderNotes = dto.Notes;
        handover.RespondedAt = DateTime.UtcNow;
        handover.IsAccepted = dto.Accept;

        if (dto.Accept)
        {
            booking.Status = BookingStatus.Active;

            await _uow.Notifications.AddAsync(new Notification
            {
                UserId = booking.Tool.OwnerId,
                Type = NotificationType.HandoverAccepted,
                Content = $"The renter has accepted the delivery of '{booking.Tool.Name}'. Rental is now active.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            booking.Status = BookingStatus.Disputed;

            // Auto-create issue report
            await _uow.IssueReports.AddAsync(new IssueReport
            {
                BookingId = bookingId,
                ReporterId = renterId,
                Title = "Delivery Handover Issue",
                Description = dto.Notes ?? "The renter has reported an issue with the delivered tool.",
                Status = IssueReportStatus.Open,
                CreatedAt = DateTime.UtcNow
            });

            await _uow.Notifications.AddAsync(new Notification
            {
                UserId = booking.Tool.OwnerId,
                Type = NotificationType.HandoverDisputed,
                Content = $"The renter has reported an issue with the delivery of '{booking.Tool.Name}'.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _uow.SaveChangesAsync();
    }

    // ── Submit Return Handover (Renter) ─────────────────────────────────────
    public async Task SubmitReturnHandoverAsync(int bookingId, int renterId, SubmitHandoverRequest dto)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.RenterId != renterId)
            throw new UnauthorizedAccessException("Only the renter can submit a return handover.");

        if (booking.Status != BookingStatus.Active)
            throw new InvalidOperationException("Booking must be active before submitting return.");

        var handover = new Handover
        {
            BookingId = bookingId,
            Type = HandoverType.Return,
            SubmittedByUserId = renterId,
            SubmitterNotes = dto.Notes,
            SubmittedAt = DateTime.UtcNow
        };

        await _uow.Handovers.AddAsync(handover);
        booking.Status = BookingStatus.ReturnHandover;

        // Save handover images
        await SaveHandoverImagesAsync(handover, dto);

        await _uow.Notifications.AddAsync(new Notification
        {
            UserId = booking.Tool.OwnerId,
            Type = NotificationType.HandoverPending,
            Content = $"The renter has submitted a return for '{booking.Tool.Name}'. Please inspect and confirm.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _uow.SaveChangesAsync();
    }

    // ── Respond to Return Handover (Owner) ──────────────────────────────────
    public async Task RespondReturnHandoverAsync(int bookingId, int ownerId, RespondHandoverRequest dto)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.Tool.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Only the tool owner can respond to a return handover.");

        if (booking.Status != BookingStatus.ReturnHandover)
            throw new InvalidOperationException("Booking is not in return handover state.");

        var handover = await _uow.Handovers.GetPendingHandoverAsync(bookingId, HandoverType.Return)
            ?? throw new InvalidOperationException("No pending return handover found.");

        handover.RespondedByUserId = ownerId;
        handover.ResponderNotes = dto.Notes;
        handover.RespondedAt = DateTime.UtcNow;
        handover.IsAccepted = dto.Accept;

        if (dto.Accept)
        {
            await SettleBookingAsync(booking, handover);

            await _uow.Notifications.AddAsync(new Notification
            {
                UserId = booking.RenterId,
                Type = NotificationType.HandoverAccepted,
                Content = $"The owner has accepted the return of '{booking.Tool.Name}'. Booking completed.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            booking.Status = BookingStatus.Disputed;

            // Auto-create issue report
            await _uow.IssueReports.AddAsync(new IssueReport
            {
                BookingId = bookingId,
                ReporterId = ownerId,
                Title = "Return Handover Issue",
                Description = dto.Notes ?? "The owner has reported an issue with the returned tool.",
                Status = IssueReportStatus.Open,
                CreatedAt = DateTime.UtcNow
            });

            await _uow.Notifications.AddAsync(new Notification
            {
                UserId = booking.RenterId,
                Type = NotificationType.HandoverDisputed,
                Content = $"The owner has reported an issue with the return of '{booking.Tool.Name}'.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _uow.SaveChangesAsync();
    }

    // ── Queries ─────────────────────────────────────────────────────────────
    public async Task<IEnumerable<BookingCardDto>> GetMyBookingsAsync(int renterId)
    {
        var bookings = await _uow.Bookings.GetByRenterIdAsync(renterId);
        return bookings.Select(b => MapToCardDto(b, renterId, isOwner: false));
    }

    public async Task<IEnumerable<BookingCardDto>> GetReceivedBookingsAsync(int ownerId)
    {
        var bookings = await _uow.Bookings.GetByOwnerIdAsync(ownerId);
        return bookings.Select(b => MapToCardDto(b, ownerId, isOwner: true));
    }

    public async Task<BookingDto> GetBookingByIdAsync(int bookingId, int requestingUserId)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.RenterId != requestingUserId && booking.Tool.OwnerId != requestingUserId)
            throw new UnauthorizedAccessException("You are not authorized to view this booking.");

        var isOwner = booking.Tool.OwnerId == requestingUserId;
        return MapToDto(booking, requestingUserId, isOwner);
    }

    // ── Private Helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Full financial settlement when return is accepted.
    /// Calculates late fee (if any), credits owner, refunds insurance.
    /// </summary>
    private async Task SettleBookingAsync(Booking booking, Handover returnHandover)
    {
        var days = (int)(booking.EndDate.Date - booking.StartDate.Date).TotalDays;
        var rentalCost = days * booking.Tool.PricePerDay;
        var platformFee = Math.Round(rentalCost * (_platform.FeePercent / 100m), 2);
        var ownerEarning = rentalCost - platformFee;
        var insuranceAmount = booking.Tool.InsurancePrice;

        // Late fee calculation
        var lateFee = 0m;
        if (returnHandover.SubmittedAt > booking.EndDate)
        {
            var lateHours = Math.Ceiling((decimal)(returnHandover.SubmittedAt - booking.EndDate).TotalHours);
            var hourlyRate = booking.Tool.PricePerDay / 24m;
            lateFee = Math.Min(lateHours * hourlyRate, insuranceAmount);
            lateFee = Math.Round(lateFee, 2);
        }

        var insuranceRefund = insuranceAmount - lateFee;

        // Credit owner: earnings + late fee
        var ownerWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.Tool.OwnerId);

        ownerWallet.Balance += ownerEarning;
        ownerWallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = ownerWallet.Id,
            Amount = ownerEarning,
            Type = WalletTransactionType.EarningCredit,
            Reference = booking.Id.ToString(),
            Description = $"Earnings for booking #{booking.Id} (after {_platform.FeePercent}% platform fee)",
            CreatedAt = DateTime.UtcNow
        });

        if (lateFee > 0)
        {
            ownerWallet.Balance += lateFee;

            await _uow.WalletTransactions.AddAsync(new WalletTransaction
            {
                WalletId = ownerWallet.Id,
                Amount = lateFee,
                Type = WalletTransactionType.LateFeeCredit,
                Reference = booking.Id.ToString(),
                Description = $"Late return fee for booking #{booking.Id}",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Refund insurance (minus late fee) to renter
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
                Reference = booking.Id.ToString(),
                Description = lateFee > 0
                    ? $"Insurance refund for booking #{booking.Id} (late fee of {lateFee:C} deducted)"
                    : $"Insurance refund for completed booking #{booking.Id}",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Update booking status
        booking.Status = BookingStatus.Completed;
        booking.CompletedAt = DateTime.UtcNow;
        booking.Tool.IsAvailable = true;

        if (booking.Payment != null)
        {
            booking.Payment.EscrowStatus = EscrowStatus.Released;
        }

        // Cleanup handover images from DB and file system
        await CleanupHandoverImagesAsync(booking.Id);
    }

    /// <summary>
    /// Full refund to renter wallet from held balance.
    /// </summary>
    private async Task RefundRenterAsync(Booking booking)
    {
        var renterWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.RenterId);

        renterWallet.Balance += booking.TotalPrice;
        renterWallet.HeldBalance -= booking.TotalPrice;
        renterWallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = renterWallet.Id,
            Amount = booking.TotalPrice,
            Type = WalletTransactionType.BookingRefund,
            Reference = booking.Id.ToString(),
            Description = $"Refund for booking #{booking.Id}",
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Save uploaded handover images to disk and create DB records.
    /// Files are saved to wwwroot/uploads/handover/{bookingId}/{unique_filename}.
    /// </summary>
    private async Task SaveHandoverImagesAsync(Handover handover, SubmitHandoverRequest dto)
    {
        if (dto.Images == null || dto.Images.Count == 0)
            return;

        var folderPath = Path.Combine(_handoverUploadPath, handover.BookingId.ToString());
        Directory.CreateDirectory(folderPath);

        foreach (var file in dto.Images)
        {
            if (file.Length == 0) continue;

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(folderPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            var relativeUrl = $"/uploads/handover/{handover.BookingId}/{fileName}";

            await _uow.HandoverImages.AddAsync(new HandoverImage
            {
                Handover = handover,
                ImageUrl = relativeUrl,
                Caption = null,
                UploadedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Delete all handover images for a booking (DB records + physical files).
    /// </summary>
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

    // ── Allowed Actions ─────────────────────────────────────────────────────

    /// <summary>
    /// Compute the list of actions the current user is allowed to perform on this booking.
    /// </summary>
    private static IReadOnlyList<string> ComputeAllowedActions(Booking b, int userId, bool isOwner)
    {
        var actions = new List<string>();

        switch (b.Status)
        {
            case BookingStatus.Pending:
                if (isOwner)
                {
                    actions.Add("Accept");
                    actions.Add("Reject");
                    actions.Add("MessageRenter");
                }
                else
                {
                    actions.Add("Cancel");
                    actions.Add("MessageOwner");
                }
                break;

            case BookingStatus.Accepted:
                if (isOwner)
                {
                    actions.Add("SubmitDeliveryHandover");
                    actions.Add("Cancel");
                    actions.Add("MessageRenter");
                }
                else
                {
                    actions.Add("Cancel");
                    actions.Add("MessageOwner");
                }
                break;

            case BookingStatus.DeliveryHandover:
                if (isOwner)
                {
                    actions.Add("MessageRenter");
                }
                else
                {
                    actions.Add("RespondDeliveryHandover");
                    actions.Add("MessageOwner");
                }
                break;

            case BookingStatus.Active:
                if (isOwner)
                {
                    actions.Add("ReportIssue");
                    actions.Add("MessageRenter");
                }
                else
                {
                    actions.Add("SubmitReturnHandover");
                    actions.Add("ReportIssue");
                    actions.Add("MessageOwner");
                }
                break;

            case BookingStatus.ReturnHandover:
                if (isOwner)
                {
                    actions.Add("RespondReturnHandover");
                    actions.Add("MessageRenter");
                }
                else
                {
                    actions.Add("MessageOwner");
                }
                break;

            case BookingStatus.Completed:
                if (isOwner)
                {
                    actions.Add("MessageRenter");
                }
                else
                {
                    // Check if renter already left a review
                    var hasReview = b.Reviews?.Any() ?? false;
                    if (!hasReview)
                        actions.Add("LeaveReview");
                    actions.Add("MessageOwner");
                }
                break;

            case BookingStatus.Disputed:
                if (isOwner)
                    actions.Add("MessageRenter");
                else
                    actions.Add("MessageOwner");
                break;

            // Cancelled / Rejected — no actions
            case BookingStatus.Cancelled:
            case BookingStatus.Rejected:
                break;
        }

        return actions;
    }

    // ── Mapping — Card DTO (for list views) ─────────────────────────────────

    private static BookingCardDto MapToCardDto(Booking b, int userId, bool isOwner)
    {
        var days = (int)(b.EndDate.Date - b.StartDate.Date).TotalDays;

        // Determine the "other party" based on perspective
        int otherPartyId;
        string otherPartyName;
        string? otherPartyImageUrl;

        if (isOwner)
        {
            otherPartyId = b.RenterId;
            otherPartyName = b.Renter?.FullName ?? string.Empty;
            otherPartyImageUrl = b.Renter?.ProfileImageUrl;
        }
        else
        {
            otherPartyId = b.Tool?.OwnerId ?? 0;
            otherPartyName = b.Tool?.Owner?.FullName ?? string.Empty;
            otherPartyImageUrl = b.Tool?.Owner?.ProfileImageUrl;
        }

        // Build handover summaries
        var deliveryHandover = b.Handovers?
            .Where(h => h.Type == HandoverType.Delivery)
            .OrderByDescending(h => h.SubmittedAt)
            .Select(h => new HandoverSummaryDto(
                Id: h.Id,
                IsSubmitted: true,
                IsAccepted: h.IsAccepted,
                SubmittedAt: h.SubmittedAt,
                RespondedAt: h.RespondedAt,
                ImageCount: h.Images?.Count ?? 0
            ))
            .FirstOrDefault();

        var returnHandover = b.Handovers?
            .Where(h => h.Type == HandoverType.Return)
            .OrderByDescending(h => h.SubmittedAt)
            .Select(h => new HandoverSummaryDto(
                Id: h.Id,
                IsSubmitted: true,
                IsAccepted: h.IsAccepted,
                SubmittedAt: h.SubmittedAt,
                RespondedAt: h.RespondedAt,
                ImageCount: h.Images?.Count ?? 0
            ))
            .FirstOrDefault();

        return new BookingCardDto(
            Id: b.Id,
            ToolId: b.ToolId,
            ToolName: b.Tool?.Name ?? string.Empty,
            ToolImageUrl: b.Tool?.Images?.FirstOrDefault()?.ImageUrl,
            OtherPartyId: otherPartyId,
            OtherPartyName: otherPartyName,
            OtherPartyImageUrl: otherPartyImageUrl,
            StartDate: b.StartDate,
            EndDate: b.EndDate,
            Days: days,
            TotalPrice: b.TotalPrice,
            Status: b.Status?.ToString() ?? string.Empty,
            CreatedAt: b.CreatedAt,
            DeliveryHandover: deliveryHandover,
            ReturnHandover: returnHandover,
            AllowedActions: ComputeAllowedActions(b, userId, isOwner)
        );
    }

    // ── Mapping — Full DTO (for detail view) ────────────────────────────────

    private static BookingDto MapToDto(Booking b, int requestingUserId, bool isOwner)
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
            AllowedActions: ComputeAllowedActions(b, requestingUserId, isOwner),
            HasReview: b.Reviews?.Any() ?? false);
    }
}
