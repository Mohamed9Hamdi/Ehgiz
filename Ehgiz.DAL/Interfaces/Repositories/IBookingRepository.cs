using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    Task<bool> HasOverlappingBookingAsync(int toolId, DateTime startDate, DateTime endDate);
}
