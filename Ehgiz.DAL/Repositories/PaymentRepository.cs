using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces.Repositories;

namespace Ehgiz.DAL.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(EhgizDbContext context) : base(context)
    {
    }
}
