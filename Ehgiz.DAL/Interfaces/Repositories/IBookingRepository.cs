using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    Task<bool> HasOverlappingBookingAsync(int toolId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get bookings where the user is the renter, with Tool, Tool.Owner, Tool.Images, Handovers included.
    /// </summary>
    Task<IReadOnlyList<Booking>> GetByRenterIdAsync(int renterId);

    /// <summary>
    /// Get bookings where the user is the tool owner, with Tool, Tool.Images, Renter, Handovers included.
    /// </summary>
    Task<IReadOnlyList<Booking>> GetByOwnerIdAsync(int ownerId);

    /// <summary>
    /// Get a single booking with all related data for the detail view.
    /// </summary>
    Task<Booking?> GetBookingWithDetailsAsync(int bookingId);

    /// <summary>
    /// Get booked date ranges for a tool within a date range (for calendar view).
    /// </summary>
    Task<IReadOnlyList<Booking>> GetBookedDatesByToolIdAsync(int toolId, DateTime from, DateTime to);

    /// <summary>
    /// Get all disputed bookings with full details for admin.
    /// </summary>
    Task<IReadOnlyList<Booking>> GetDisputedBookingsAsync();
}
