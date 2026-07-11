using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Admin;
using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.DTOs.Handovers;
using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminService(
        IUnitOfWork uow,
        ICloudinaryService cloudinaryService,
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager)
    {
        _uow = uow;
        _notificationService = notificationService;
        _cloudinaryService = cloudinaryService;
        _userManager = userManager;
    }

    // ── Dashboard ───────────────────────────────────────────────────────────

    public async Task<AdminDashboardStatsDto> GetDashboardStatsAsync()
    {
        var totalUsers = await _uow.Users.CountAsync();
        var activeUsers = await _uow.Users.CountAsync(u => u.IsActive);
        var totalListings = await _uow.Tools.CountAsync();
        var activeListings = await _uow.Tools.CountAsync(t => t.IsAvailable);
        var totalBookings = await _uow.Bookings.CountAsync();
        var activeBookings = await _uow.Bookings.CountAsync(b => b.Status == BookingStatus.Active);
        var disputedBookings = await _uow.Bookings.CountAsync(b => b.Status == BookingStatus.Disputed);
        var openIssues = await _uow.IssueReports.CountAsync(ir => ir.Status == IssueReportStatus.Open);
        var totalCategories = await _uow.Categories.CountAsync();

        var totalRevenue = await _uow.PlatformRevenueLedgers.Query().SumAsync(r => r.Amount);
        var pendingEscrow = await _uow.Wallets.Query().SumAsync(w => w.HeldBalance);

        return new AdminDashboardStatsDto(
            TotalUsers: totalUsers,
            ActiveUsers: activeUsers,
            TotalListings: totalListings,
            ActiveListings: activeListings,
            TotalBookings: totalBookings,
            ActiveBookings: activeBookings,
            DisputedBookings: disputedBookings,
            OpenIssueReports: openIssues,
            TotalCategories: totalCategories,
            TotalRevenue: totalRevenue,
            PendingEscrow: pendingEscrow);
    }

    // ── List Disputed Bookings ──────────────────────────────────────────────

    public async Task<IEnumerable<BookingDto>> GetDisputedBookingsAsync()
    {
        var disputed = await _uow.Bookings.GetDisputedBookingsAsync();
        return disputed.Adapt<List<BookingDto>>();
    }

    // ── Get Dispute Details ─────────────────────────────────────────────────

    public async Task<DisputeDetailsDto> GetDisputeDetailsAsync(int bookingId)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.Status != BookingStatus.Disputed)
            throw new InvalidOperationException("This booking is not in a disputed state.");

        var bookingDto = booking.Adapt<BookingDto>();
        var issues = booking.IssueReports.Adapt<List<IssueReportDto>>();
        var handovers = booking.Handovers.Adapt<List<HandoverDto>>();

        return new DisputeDetailsDto(bookingDto, issues, handovers);
    }

    // ── 1. Resolve in Favor of Owner ────────────────────────────────────────

    public async Task ResolveInFavorOfOwnerAsync(int bookingId, ResolveDisputeRequest dto)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            var ownerWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.Tool.OwnerId);
            var renterWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.RenterId);

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

            if (booking.Payment != null)
            {
                booking.Payment.EscrowStatus = EscrowStatus.Released;
                booking.Payment.PaymentStatus = PaymentStatus.Completed;
            }

            FinalizeDispute(booking, BookingStatus.Completed, dto.ResolutionNotes);
            await CleanupHandoverImagesAsync(bookingId);
            await _uow.SaveChangesAsync();
        });

        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = booking.Tool.OwnerId,
            Title = "Dispute Resolved in Your Favor",
            Message = $"The dispute for booking #{bookingId} has been resolved in your favor. Funds have been transferred to your wallet.",
            Type = NotificationType.DisputeResolved,
            Url = $"/bookings/{bookingId}"
        });
        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = booking.RenterId,
            Title = "Dispute Resolved",
            Message = $"The dispute for booking #{bookingId} has been resolved. The escrow funds were awarded to the owner.",
            Type = NotificationType.DisputeResolved,
            Url = $"/bookings/{bookingId}"
        });
    }

    // ── 2. Resolve in Favor of Renter ───────────────────────────────────────

    public async Task ResolveInFavorOfRenterAsync(int bookingId, ResolveDisputeRequest dto)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        await _uow.ExecuteInTransactionAsync(async () =>
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
        });

        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = booking.RenterId,
            Title = "Dispute Resolved in Your Favor",
            Message = $"The dispute for booking #{bookingId} has been resolved in your favor. A full refund has been issued to your wallet.",
            Type = NotificationType.DisputeResolved,
            Url = $"/bookings/{bookingId}"
        });
        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = booking.Tool.OwnerId,
            Title = "Dispute Resolved",
            Message = $"The dispute for booking #{bookingId} has been resolved. A refund was issued to the renter.",
            Type = NotificationType.DisputeResolved,
            Url = $"/bookings/{bookingId}"
        });
    }

    // ── 3. Partial Refund ───────────────────────────────────────────────────

    public async Task ResolvePartialRefundAsync(int bookingId, PartialRefundRequest dto)
    {
        if (dto.RefundPercentage < 1 || dto.RefundPercentage > 99)
            throw new InvalidOperationException("Refund percentage must be between 1 and 99.");

        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            var refundAmount = Math.Round(booking.TotalPrice * (dto.RefundPercentage / 100m), 2);
            var ownerAmount = booking.TotalPrice - refundAmount;

            var renterWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.RenterId);
            var ownerWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(booking.Tool.OwnerId);

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
                booking.Payment.PaymentStatus = PaymentStatus.Completed;
            }

            FinalizeDispute(booking, BookingStatus.Completed, dto.ResolutionNotes);
            await CleanupHandoverImagesAsync(bookingId);
            await _uow.SaveChangesAsync();
        });

        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = booking.RenterId,
            Title = "Dispute Resolved – Partial Refund",
            Message = $"The dispute for booking #{bookingId} was resolved. You received a {dto.RefundPercentage}% refund.",
            Type = NotificationType.DisputeResolved,
            Url = $"/bookings/{bookingId}"
        });
        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = booking.Tool.OwnerId,
            Title = "Dispute Resolved – Partial Payout",
            Message = $"The dispute for booking #{bookingId} was resolved. You received {100 - dto.RefundPercentage}% of the escrow amount.",
            Type = NotificationType.DisputeResolved,
            Url = $"/bookings/{bookingId}"
        });
    }

    // ── 4. Force Complete ───────────────────────────────────────────────────

    public async Task ForceCompleteAsync(int bookingId, ResolveDisputeRequest dto)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            var ownerEarning = booking.RentalCost - booking.PlatformFee;
            var insuranceAmount = booking.InsuranceAmount;

            var lateFee = 0m;
            var returnHandover = booking.Handovers.FirstOrDefault(h => h.Type == HandoverType.Return);

            if (returnHandover != null && returnHandover.SubmittedAt > booking.EndDate)
            {
                var lateHours = Math.Ceiling((decimal)(returnHandover.SubmittedAt - booking.EndDate).TotalHours);
                var hourlyRate = booking.PricePerDay / 24m;
                lateFee = Math.Min(lateHours * hourlyRate, insuranceAmount);
                lateFee = Math.Round(lateFee, 2);
            }

            var insuranceRefund = insuranceAmount - lateFee;

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
                booking.Payment.PaymentStatus = PaymentStatus.Completed;
            }

            FinalizeDispute(booking, BookingStatus.Completed, dto.ResolutionNotes);
            await CleanupHandoverImagesAsync(bookingId);
            await _uow.SaveChangesAsync();
        });

        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = booking.RenterId,
            Title = "Booking Force-Completed",
            Message = $"Admin has force-completed booking #{bookingId}. Any applicable insurance refund has been credited to your wallet.",
            Type = NotificationType.DisputeResolved,
            Url = $"/bookings/{bookingId}"
        });
        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = booking.Tool.OwnerId,
            Title = "Booking Force-Completed – Earnings Credited",
            Message = $"Admin has force-completed booking #{bookingId}. Your earnings have been credited to your wallet.",
            Type = NotificationType.DisputeResolved,
            Url = $"/bookings/{bookingId}"
        });
    }

    // ── 5. Force Cancel ─────────────────────────────────────────────────────

    public async Task ForceCancelAsync(int bookingId, ResolveDisputeRequest dto)
    {
        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        ValidateDisputed(booking);

        await _uow.ExecuteInTransactionAsync(async () =>
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
        });

        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = booking.RenterId,
            Title = "Booking Force-Cancelled – Full Refund",
            Message = $"Admin has force-cancelled booking #{bookingId}. A full refund has been issued to your wallet.",
            Type = NotificationType.DisputeResolved,
            Url = $"/bookings/{bookingId}"
        });
        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = booking.Tool.OwnerId,
            Title = "Booking Force-Cancelled",
            Message = $"Admin has force-cancelled booking #{bookingId}. The escrow funds were refunded to the renter.",
            Type = NotificationType.DisputeResolved,
            Url = $"/bookings/{bookingId}"
        });
    }

    // ── Issue Report Management ─────────────────────────────────────────────

    public async Task<IEnumerable<IssueReportDto>> GetIssueReportsAsync()
    {
        return await _uow.IssueReports.Query()
            .OrderByDescending(ir => ir.CreatedAt)
            .ProjectToType<IssueReportDto>()
            .ToListAsync();
    }

    public async Task<IssueReportDto> GetIssueReportByIdAsync(int id)
    {
        return await _uow.IssueReports.Query()
            .Where(ir => ir.Id == id)
            .ProjectToType<IssueReportDto>()
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Issue report {id} not found.");
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

        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = ir.ReporterId,
            Title = "Issue Report Updated",
            Message = $"Your issue report '{ir.Title}' status has been updated to: {newStatus}.",
            Type = NotificationType.IssueReport,
            Url = $"/bookings/{ir.BookingId}"
        });
    }

    // ── User Management ─────────────────────────────────────────────────────

    public async Task<IEnumerable<AdminUserDetailsDto>> GetUsersAsync()
    {
        var users = await _userManager.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
        if (users.Count == 0) return [];

        var userIds = users.Select(u => u.Id).ToList();

        var listingCounts = await _uow.Tools.Query()
            .Where(t => userIds.Contains(t.OwnerId))
            .GroupBy(t => t.OwnerId)
            .Select(g => new { OwnerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OwnerId, x => x.Count);

        var bookingCounts = await _uow.Bookings.Query()
            .Where(b => userIds.Contains(b.RenterId))
            .GroupBy(b => b.RenterId)
            .Select(g => new { RenterId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RenterId, x => x.Count);

        var result = new List<AdminUserDetailsDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(user.Adapt<AdminUserDetailsDto>() with
            {
                Role = roles.FirstOrDefault() ?? AppRoles.User,
                TotalListings = listingCounts.GetValueOrDefault(user.Id, 0),
                TotalBookings = bookingCounts.GetValueOrDefault(user.Id, 0)
            });
        }

        return result;
    }

    public async Task<AdminUserDetailsDto> GetUserByIdAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        var roles = await _userManager.GetRolesAsync(user);
        var totalListings = await _uow.Tools.CountAsync(t => t.OwnerId == userId);
        var totalBookings = await _uow.Bookings.CountAsync(b => b.RenterId == userId);

        return user.Adapt<AdminUserDetailsDto>() with
        {
            Role = roles.FirstOrDefault() ?? AppRoles.User,
            TotalListings = totalListings,
            TotalBookings = totalBookings
        };
    }

    public async Task SetUserActiveAsync(int userId, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        user.IsActive = isActive;
        await _userManager.UpdateAsync(user);

        if (!isActive)
        {
            // Kill existing sessions so the user cannot refresh into a new access token.
            await _uow.RefreshTokens.RevokeAllActiveByUserIdAsync(userId);
            await _uow.SaveChangesAsync();
        }
    }

    public async Task SetUserRoleAsync(int userId, string role)
    {
        if (!AppRoles.All.Contains(role, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Invalid role '{role}'. Valid values: {string.Join(", ", AppRoles.All)}");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);
    }

    public async Task DeleteUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        var hasActiveBookings = await _uow.Bookings.CountAsync(b =>
            b.RenterId == userId &&
            (b.Status == BookingStatus.Pending ||
             b.Status == BookingStatus.Accepted ||
             b.Status == BookingStatus.Active)) > 0;

        if (hasActiveBookings)
            throw new InvalidOperationException("Cannot delete a user that has active or pending bookings.");

        var hasActiveListings = await _uow.Bookings.CountAsync(b =>
            b.Tool.OwnerId == userId &&
            (b.Status == BookingStatus.Pending ||
             b.Status == BookingStatus.Accepted ||
             b.Status == BookingStatus.Active)) > 0;

        if (hasActiveListings)
            throw new InvalidOperationException("Cannot delete a user that owns listings with active or pending bookings.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to delete user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    // ── Listing Management ──────────────────────────────────────────────────

    public async Task<IEnumerable<AdminListingDto>> GetListingsAsync()
    {
        return await _uow.Tools.Query()
            .OrderByDescending(t => t.CreatedAt)
            .ProjectToType<AdminListingDto>()
            .ToListAsync();
    }

    public async Task<AdminListingDetailsDto> GetListingByIdAsync(int id)
    {
        var listing = await _uow.Tools.Query()
            .Where(t => t.Id == id)
            .ProjectToType<AdminListingDetailsDto>()
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Listing {id} not found.");

        var totalBookings = await _uow.Bookings.CountAsync(b => b.ToolId == id);

        return listing with { TotalBookings = totalBookings };
    }

    public async Task SetListingAvailabilityAsync(int id, bool isAvailable)
    {
        var tool = await _uow.Tools.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Listing {id} not found.");

        tool.IsAvailable = isAvailable;
        tool.UpdatedAt = DateTime.UtcNow;
        _uow.Tools.Update(tool);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteListingAsync(int id)
    {
        var tool = await _uow.Tools.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Listing {id} not found.");

        var hasActiveBookings = await _uow.Bookings.CountAsync(b =>
            b.ToolId == id &&
            (b.Status == BookingStatus.Pending ||
             b.Status == BookingStatus.Accepted ||
             b.Status == BookingStatus.Active)) > 0;

        if (hasActiveBookings)
            throw new InvalidOperationException("Cannot delete a listing that has active or pending bookings.");

        _uow.Tools.Remove(tool);
        await _uow.SaveChangesAsync();
    }

    // ── Category Management ─────────────────────────────────────────────────

    public async Task<IEnumerable<AdminCategoryDto>> GetCategoriesAsync()
    {
        return await _uow.Categories.Query()
            .Select(c => new AdminCategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.ImageUrl,
                c.IsActive,
                c.Tools.Count()))
            .ToListAsync();
    }

    public async Task<AdminCategoryDto> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            IsActive = true
        };

        await _uow.Categories.AddAsync(category);
        await _uow.SaveChangesAsync();

        return new AdminCategoryDto(category.Id, category.Name, category.Description, category.ImageUrl, category.IsActive, 0);
    }

    public async Task<AdminCategoryDto> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
    {
        var category = await _uow.Categories.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

        if (request.Name is not null) category.Name = request.Name;
        if (request.Description is not null) category.Description = request.Description;
        if (request.ImageUrl is not null) category.ImageUrl = request.ImageUrl;
        if (request.IsActive is not null) category.IsActive = request.IsActive.Value;

        _uow.Categories.Update(category);
        await _uow.SaveChangesAsync();

        var toolCount = await _uow.Tools.CountAsync(t => t.CategoryId == category.Id);
        return new AdminCategoryDto(category.Id, category.Name, category.Description, category.ImageUrl, category.IsActive, toolCount);
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _uow.Categories.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

        var hasTools = await _uow.Tools.CountAsync(t => t.CategoryId == id) > 0;
        if (hasTools)
            throw new InvalidOperationException("Cannot delete a category that has listings assigned to it.");

        _uow.Categories.Remove(category);
        await _uow.SaveChangesAsync();
    }

    public async Task<string> UploadCategoryImageAsync(Microsoft.AspNetCore.Http.IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File is empty.");

        var uploadResult = await _cloudinaryService.UploadImageAsync(file);
        return uploadResult.ImageUrl;
    }

    // ── Platform Settings ───────────────────────────────────────────────────

    public async Task<decimal> GetPlatformFeeAsync()
    {
        var setting = await _uow.SystemSettings.GetByIdAsync("PlatformFeePercent");
        if (setting != null && decimal.TryParse(setting.Value, out var fee))
            return fee;
        return 10m;
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

    // ── Wallet & Transaction Management ────────────────────────────────────

    public async Task<IEnumerable<AdminWalletDto>> GetWalletsAsync()
    {
        return await _uow.Wallets.Query()
            .OrderByDescending(w => w.UpdatedAt)
            .ProjectToType<AdminWalletDto>()
            .ToListAsync();
    }

    public async Task<IEnumerable<AdminWalletTransactionDto>> GetAllTransactionsAsync()
    {
        return await _uow.WalletTransactions.Query()
            .OrderByDescending(t => t.CreatedAt)
            .ProjectToType<AdminWalletTransactionDto>()
            .ToListAsync();
    }

    // Booking-settlement transaction types whose Reference reliably points at a BookingId.
    // (BookingDebit is excluded — its Reference holds the ToolId, set before the booking
    // itself exists, so it cannot be traced back to a specific booking safely.)
    private static readonly WalletTransactionType[] ReversibleTransactionTypes =
    [
        WalletTransactionType.EarningCredit,
        WalletTransactionType.InsuranceRefund,
        WalletTransactionType.BookingRefund,
        WalletTransactionType.LateFeeCredit,
        WalletTransactionType.PartialRefund,
        WalletTransactionType.DisputeCredit
    ];

    public async Task<RollbackTransactionResultDto> RollbackTransactionAsync(int transactionId, RollbackTransactionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("A reason is required to roll back a transaction.");

        var original = await _uow.WalletTransactions.Query()
            .Include(t => t.Wallet)
            .FirstOrDefaultAsync(t => t.Id == transactionId)
            ?? throw new KeyNotFoundException($"Transaction {transactionId} not found.");

        if (!ReversibleTransactionTypes.Contains(original.Type))
            throw new InvalidOperationException(
                $"Transactions of type '{original.Type}' cannot be rolled back here — only booking settlement/refund credits linked to a specific booking can be reversed this way.");

        if (string.IsNullOrWhiteSpace(original.Reference) || !int.TryParse(original.Reference, out var bookingId))
            throw new InvalidOperationException("This transaction has no linked booking and cannot be rolled back automatically.");

        var alreadyReversed = await _uow.WalletTransactions.Query()
            .AnyAsync(t => t.Type == WalletTransactionType.AdminReversal && t.Reference == $"reversal:{original.Id}");
        if (alreadyReversed)
            throw new InvalidOperationException("This transaction has already been rolled back.");

        var booking = await _uow.Bookings.GetBookingWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} linked to this transaction was not found.");

        var receiverWallet = original.Wallet;
        var receiverUserId = receiverWallet.UserId;

        if (receiverUserId != booking.RenterId && receiverUserId != booking.Tool.OwnerId)
            throw new InvalidOperationException("This transaction's wallet is not a party to the linked booking.");

        var senderUserId = receiverUserId == booking.RenterId ? booking.Tool.OwnerId : booking.RenterId;
        var amount = Math.Abs(original.Amount);

        if (original.Amount > 0 && receiverWallet.Balance < amount)
            throw new InvalidOperationException("The receiving wallet does not have enough balance to reverse this transaction.");

        var senderWallet = await _uow.Wallets.GetOrCreateByUserIdAsync(senderUserId);

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            if (original.Amount > 0)
            {
                receiverWallet.Balance -= amount;
                senderWallet.Balance += amount;
            }
            else
            {
                senderWallet.Balance -= amount;
                receiverWallet.Balance += amount;
            }

            receiverWallet.UpdatedAt = DateTime.UtcNow;
            senderWallet.UpdatedAt = DateTime.UtcNow;

            await _uow.WalletTransactions.AddAsync(new WalletTransaction
            {
                WalletId = receiverWallet.Id,
                Amount = -original.Amount,
                Type = WalletTransactionType.AdminReversal,
                Reference = $"reversal:{original.Id}",
                Description = $"Admin rollback of transaction #{original.Id}: {request.Reason}",
                CreatedAt = DateTime.UtcNow
            });

            await _uow.WalletTransactions.AddAsync(new WalletTransaction
            {
                WalletId = senderWallet.Id,
                Amount = original.Amount,
                Type = WalletTransactionType.AdminReversal,
                Reference = $"reversal:{original.Id}",
                Description = $"Admin rollback of transaction #{original.Id}: {request.Reason}",
                CreatedAt = DateTime.UtcNow
            });

            await _uow.SaveChangesAsync();
        });

        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = senderUserId,
            Title = "Transaction Reversed",
            Message = $"An admin has reversed transaction #{original.Id} related to booking #{bookingId}. {amount:C} has been returned to your wallet.",
            Type = NotificationType.DisputeResolved,
            Url = $"/bookings/{bookingId}"
        });
        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = receiverUserId,
            Title = "Transaction Reversed",
            Message = $"An admin has reversed transaction #{original.Id} related to booking #{bookingId}. {amount:C} has been deducted from your wallet.",
            Type = NotificationType.DisputeResolved,
            Url = $"/bookings/{bookingId}"
        });

        return new RollbackTransactionResultDto(
            OriginalTransactionId: original.Id,
            SenderUserId: senderUserId,
            ReceiverUserId: receiverUserId,
            Amount: amount,
            Reason: request.Reason,
            CreatedAt: DateTime.UtcNow);
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
        {
            if (!string.IsNullOrEmpty(image.PublicId))
            {
                await _cloudinaryService.DeleteImageAsync(image.PublicId);
            }
            _uow.HandoverImages.Remove(image);
        }
    }
}
