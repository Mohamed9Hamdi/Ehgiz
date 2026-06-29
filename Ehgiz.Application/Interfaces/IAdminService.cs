using Ehgiz.Application.DTOs.Admin;
using Ehgiz.Application.DTOs.Bookings;

namespace Ehgiz.Application.Interfaces;

public interface IAdminService
{
    // Dashboard
    Task<AdminDashboardStatsDto> GetDashboardStatsAsync();

    // Disputes
    Task<IEnumerable<BookingDto>> GetDisputedBookingsAsync();
    Task<DisputeDetailsDto> GetDisputeDetailsAsync(int bookingId);
    Task ResolveInFavorOfOwnerAsync(int bookingId, ResolveDisputeRequest dto);
    Task ResolveInFavorOfRenterAsync(int bookingId, ResolveDisputeRequest dto);
    Task ResolvePartialRefundAsync(int bookingId, PartialRefundRequest dto);
    Task ForceCompleteAsync(int bookingId, ResolveDisputeRequest dto);
    Task ForceCancelAsync(int bookingId, ResolveDisputeRequest dto);

    // Issue Reports
    Task<IEnumerable<IssueReportDto>> GetIssueReportsAsync();
    Task<IssueReportDto> GetIssueReportByIdAsync(int id);
    Task UpdateIssueReportStatusAsync(int id, UpdateIssueStatusRequest dto);

    // Users
    Task<IEnumerable<AdminUserDto>> GetUsersAsync();
    Task<AdminUserDetailsDto> GetUserByIdAsync(int userId);
    Task SetUserActiveAsync(int userId, bool isActive);
    Task SetUserRoleAsync(int userId, string role);

    // Listings
    Task<IEnumerable<AdminListingDto>> GetListingsAsync();
    Task<AdminListingDetailsDto> GetListingByIdAsync(int id);
    Task SetListingAvailabilityAsync(int id, bool isAvailable);
    Task DeleteListingAsync(int id);

    // Categories
    Task<IEnumerable<AdminCategoryDto>> GetCategoriesAsync();
    Task<AdminCategoryDto> CreateCategoryAsync(CreateCategoryRequest request);
    Task<AdminCategoryDto> UpdateCategoryAsync(int id, UpdateCategoryRequest request);
    Task DeleteCategoryAsync(int id);

    // Payments
    Task<IEnumerable<AdminPaymentDto>> GetPaymentsAsync();
    Task<AdminPaymentDto> GetPaymentByIdAsync(int id);

    // Platform Settings
    Task<decimal> GetPlatformFeeAsync();
    Task UpdatePlatformFeeAsync(decimal feePercent);
}
