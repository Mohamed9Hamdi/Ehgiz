using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IReviewRepository : IRepository<Review>
{
    Task<List<Review>> GetByToolAsync(int toolId);
    Task<Review?> GetByIdWithDetailsAsync(int id);
    Task<bool> ExistsForBookingAsync(int bookingId);
    Task<List<int>> GetRatingsByToolAsync(int toolId);
}
