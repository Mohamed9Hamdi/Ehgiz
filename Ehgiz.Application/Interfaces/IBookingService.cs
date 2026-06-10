using Ehgiz.Application.DTOs.Bookings;

namespace Ehgiz.Application.Interfaces;

public interface IBookingService
{
    Task<CreateBookingResponse> CreateBookingAsync(int renterId, CreateBookingRequest dto);
    Task<IEnumerable<BookingDto>> GetMyBookingsAsync(int renterId);
    Task<IEnumerable<BookingDto>> GetReceivedBookingsAsync(int ownerId);
    Task<BookingDto> GetBookingByIdAsync(int bookingId, int requestingUserId);
    Task CancelBookingAsync(int bookingId, int requestingUserId);
    Task CompleteBookingAsync(int bookingId, int requestingUserId);
}
