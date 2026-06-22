using Ehgiz.Application.DTOs.Admin;
using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.DAL.Enums;

namespace Ehgiz.Application.Interfaces;

public interface IAdminService
{
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

    // Platform Settings
    Task<decimal> GetPlatformFeeAsync();
    Task UpdatePlatformFeeAsync(decimal feePercent);
}
