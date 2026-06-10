using Ehgiz.Application.DTOs.Bookings;

namespace Ehgiz.Application.Interfaces;

public interface IAdminPaymentService
{
    Task<IEnumerable<BookingDto>> GetDisputedBookingsAsync();
    Task ResolveDisputeInFavorOfOwnerAsync(int bookingId);
    Task ResolveDisputeInFavorOfRenterAsync(int bookingId);
}
