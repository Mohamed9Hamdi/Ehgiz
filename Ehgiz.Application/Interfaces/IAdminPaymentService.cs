using Ehgiz.Application.DTOs.Admin;
using Ehgiz.Application.DTOs.Bookings;

namespace Ehgiz.Application.Interfaces;

public interface IAdminPaymentService
{
    Task<IEnumerable<BookingDto>> GetDisputedBookingsAsync();
    Task<DisputeDetailsDto> GetDisputeDetailsAsync(int bookingId);
    Task ResolveInFavorOfOwnerAsync(int bookingId, ResolveDisputeRequest dto);
    Task ResolveInFavorOfRenterAsync(int bookingId, ResolveDisputeRequest dto);
    Task ResolvePartialRefundAsync(int bookingId, PartialRefundRequest dto);
    Task ForceCompleteAsync(int bookingId, ResolveDisputeRequest dto);
    Task ForceCancelAsync(int bookingId, ResolveDisputeRequest dto);
}
