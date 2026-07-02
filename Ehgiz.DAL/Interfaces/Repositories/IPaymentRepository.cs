using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByBookingIdAsync(int bookingId);

    Task<IReadOnlyList<Payment>> GetAllWithDetailsAsync();

    Task<Payment?> GetByIdWithDetailsAsync(int id);
}
