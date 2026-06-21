using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.DTOs.Handovers;

namespace Ehgiz.Application.Interfaces;

public interface IBookingService
{
    Task<CreateBookingResponse> CreateBookingAsync(int renterId, CreateBookingRequest dto);
    Task<IEnumerable<BookingCardDto>> GetMyBookingsAsync(int renterId);
    Task<IEnumerable<BookingCardDto>> GetReceivedBookingsAsync(int ownerId);
    Task<BookingDto> GetBookingByIdAsync(int bookingId, int requestingUserId);
    Task CancelBookingAsync(int bookingId, int requestingUserId);

    // Owner accept/reject
    Task AcceptBookingAsync(int bookingId, int ownerId);
    Task RejectBookingAsync(int bookingId, int ownerId);

    // Handover
    Task SubmitDeliveryHandoverAsync(int bookingId, int ownerId, SubmitHandoverRequest dto);
    Task RespondDeliveryHandoverAsync(int bookingId, int renterId, RespondHandoverRequest dto);
    Task SubmitReturnHandoverAsync(int bookingId, int renterId, SubmitHandoverRequest dto);
    Task RespondReturnHandoverAsync(int bookingId, int ownerId, RespondHandoverRequest dto);

    // Issue reporting
    Task ReportIssueAsync(int bookingId, int userId, ReportIssueRequest dto);

    // Calendar availability
    Task<ToolAvailabilityDto> GetToolAvailabilityAsync(int toolId, int year, int month);
}
