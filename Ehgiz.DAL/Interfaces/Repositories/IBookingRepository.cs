using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    Task<bool> HasOverlappingBookingAsync(int toolId, DateTime startDate, DateTime endDate);
    Task<Booking?> GetBookingWithDetailsAsync(int bookingId);
    Task<IReadOnlyList<Booking>> GetBookedDatesByToolIdAsync(int toolId, DateTime from, DateTime to);
    Task<IReadOnlyList<Booking>> GetDisputedBookingsAsync();
}
