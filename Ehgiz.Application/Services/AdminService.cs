using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Admin;
using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.DTOs.Handovers;
using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;

namespace Ehgiz.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly string _handoverUploadPath;

    public AdminService(
        IUnitOfWork uow,
        IWebHostEnvironment env,
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager)
    {
        _uow = uow;
        _notificationService = notificationService;
        _userManager = userManager;
        _handoverUploadPath = Path.Combine(env.ContentRootPath, "uploads", "handover");
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

        var allRevenue = await _uow.PlatformRevenueLedgers.GetAllAsync();
        var totalRevenue = allRevenue.Sum(r => r.Amount);

        var allWallets = await _uow.Wallets.GetAllAsync();
        var pendingEscrow = allWallets.Sum(w => w.HeldBalance);

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
        await transaction.CommitAsync();

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

        await using var transaction = await _uow.BeginTransactionAsync();

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
        await transaction.CommitAsync();

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

        await using var transaction = await _uow.BeginTransactionAsync();

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
            booking.Payment.EscrowStatus = EscrowStatus.Released;

        FinalizeDispute(booking, BookingStatus.Completed, dto.ResolutionNotes);
        await CleanupHandoverImagesAsync(bookingId);
        await _uow.SaveChangesAsync();
        await transaction.CommitAsync();

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

        await using var transaction = await _uow.BeginTransactionAsync();

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
            booking.Payment.EscrowStatus = EscrowStatus.Released;

        FinalizeDispute(booking, BookingStatus.Completed, dto.ResolutionNotes);
        await CleanupHandoverImagesAsync(bookingId);
        await _uow.SaveChangesAsync();
        await transaction.CommitAsync();

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

        await using var transaction = await _uow.BeginTransactionAsync();

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
        var all = await _uow.IssueReports.GetAllWithDetailsAsync();
        return all.Select(ir => new IssueReportDto(
            Id: ir.Id,
            ReporterName: ir.Reporter?.FullName ?? string.Empty,
            Title: ir.Title,
            Description: ir.Description,
            Status: ir.Status?.ToString() ?? string.Empty,
            CreatedAt: ir.CreatedAt));
    }

    public async Task<IssueReportDto> GetIssueReportByIdAsync(int id)
    {
        var ir = await _uow.IssueReports.GetByIdWithDetailsAsync(id)
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
        var ir = await _uow.IssueReports.GetByIdWithDetailsAsync(id)
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

    public async Task<IEnumerable<AdminUserDto>> GetUsersAsync()
    {
        var users = _userManager.Users.OrderByDescending(u => u.CreatedAt).ToList();
        var result = new List<AdminUserDto>(users.Count);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new AdminUserDto(
                Id: user.Id,
                FullName: user.FullName,
                Email: user.Email ?? string.Empty,
                ProfileImageUrl: user.ProfileImageUrl,
                City: user.City,
                IsActive: user.IsActive,
                EmailConfirmed: user.EmailConfirmed,
                Role: roles.FirstOrDefault() ?? AppRoles.User,
                CreatedAt: user.CreatedAt));
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

        return new AdminUserDetailsDto(
            Id: user.Id,
            FullName: user.FullName,
            Email: user.Email ?? string.Empty,
            ProfileImageUrl: user.ProfileImageUrl,
            Address: user.Address,
            City: user.City,
            IsActive: user.IsActive,
            EmailConfirmed: user.EmailConfirmed,
            Role: roles.FirstOrDefault() ?? AppRoles.User,
            CreatedAt: user.CreatedAt,
            TotalListings: totalListings,
            TotalBookings: totalBookings,
            StripeCustomerId: user.StripeCustomerId,
            StripeAccountId: user.StripeAccountId);
    }

    public async Task SetUserActiveAsync(int userId, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        user.IsActive = isActive;
        await _userManager.UpdateAsync(user);
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

    // ── Listing Management ──────────────────────────────────────────────────

    public async Task<IEnumerable<AdminListingDto>> GetListingsAsync()
    {
        var tools = await _uow.Tools.GetAllWithDetailsAsync();
        return tools.Select(t => new AdminListingDto(
            Id: t.Id,
            Name: t.Name,
            Description: t.Description,
            PricePerDay: t.PricePerDay,
            InsurancePrice: t.InsurancePrice,
            Condition: t.Condition?.ToString(),
            Location: t.Location,
            IsAvailable: t.IsAvailable,
            CategoryId: t.CategoryId,
            CategoryName: t.Category?.Name ?? string.Empty,
            OwnerId: t.OwnerId,
            OwnerName: t.Owner?.FullName ?? string.Empty,
            FirstImageUrl: t.Images?.FirstOrDefault()?.ImageUrl,
            CreatedAt: t.CreatedAt));
    }

    public async Task<AdminListingDetailsDto> GetListingByIdAsync(int id)
    {
        var tool = await _uow.Tools.GetByIdWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Listing {id} not found.");

        var totalBookings = await _uow.Bookings.CountAsync(b => b.ToolId == id);

        return new AdminListingDetailsDto(
            Id: tool.Id,
            Name: tool.Name,
            Description: tool.Description,
            PricePerDay: tool.PricePerDay,
            InsurancePrice: tool.InsurancePrice,
            Condition: tool.Condition?.ToString(),
            Location: tool.Location,
            IsAvailable: tool.IsAvailable,
            CategoryId: tool.CategoryId,
            CategoryName: tool.Category?.Name ?? string.Empty,
            OwnerId: tool.OwnerId,
            OwnerName: tool.Owner?.FullName ?? string.Empty,
            ImageUrls: tool.Images?.Select(i => i.ImageUrl).ToList() ?? [],
            CreatedAt: tool.CreatedAt,
            TotalBookings: totalBookings);
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
        var categories = await _uow.Categories.GetAllAsync();
        var result = new List<AdminCategoryDto>(categories.Count);

        foreach (var cat in categories)
        {
            var toolCount = await _uow.Tools.CountAsync(t => t.CategoryId == cat.Id);
            result.Add(new AdminCategoryDto(cat.Id, cat.Name, cat.Description, cat.ImageUrl, cat.IsActive, toolCount));
        }

        return result;
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

    // ── Payment Management ──────────────────────────────────────────────────

    public async Task<IEnumerable<AdminPaymentDto>> GetPaymentsAsync()
    {
        var payments = await _uow.Payments.GetAllWithDetailsAsync();
        return payments.Select(MapPaymentToDto);
    }

    public async Task<AdminPaymentDto> GetPaymentByIdAsync(int id)
    {
        var payment = await _uow.Payments.GetByIdWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Payment {id} not found.");
        return MapPaymentToDto(payment);
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

        var folderPath = Path.Combine(_handoverUploadPath, bookingId.ToString());
        if (Directory.Exists(folderPath))
            Directory.Delete(folderPath, recursive: true);
    }

    private static AdminPaymentDto MapPaymentToDto(Payment p) => new(
        Id: p.Id,
        BookingId: p.BookingId,
        RenterName: p.Booking?.Renter?.FullName ?? string.Empty,
        OwnerName: p.Booking?.Tool?.Owner?.FullName ?? string.Empty,
        ToolName: p.Booking?.Tool?.Name ?? string.Empty,
        Amount: p.Amount,
        PaymentMethod: p.PaymentMethod?.ToString(),
        PaymentStatus: p.PaymentStatus?.ToString(),
        EscrowStatus: p.EscrowStatus?.ToString(),
        StripePaymentIntentId: p.StripePaymentIntentId,
        PaidAt: p.PaidAt);

    private static BookingDto MapToDto(Booking b)
    {
        var days = (int)(b.EndDate.Date - b.StartDate.Date).TotalDays;

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
            RentalCost: b.RentalCost,
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
