using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IReviewRepository : IRepository<Review>
{
    Task<bool> ExistsForBookingAsync(int bookingId);
    Task<List<int>> GetRatingsByToolAsync(int toolId);
}
