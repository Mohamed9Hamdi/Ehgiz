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

    public async Task<IReadOnlyList<Payment>> GetAllWithDetailsAsync()
    {
        return await _context.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b.Renter)
            .Include(p => p.Booking)
                .ThenInclude(b => b.Tool)
                    .ThenInclude(t => t.Owner)
            .AsNoTracking()
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();
    }

    public async Task<Payment?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b.Renter)
            .Include(p => p.Booking)
                .ThenInclude(b => b.Tool)
                    .ThenInclude(t => t.Owner)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}
