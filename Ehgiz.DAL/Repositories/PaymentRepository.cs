using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(EhgizDbContext context) : base(context)
    {
    }

    public Task<Payment?> GetByBookingIdAsync(int bookingId) =>
        _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);
}
