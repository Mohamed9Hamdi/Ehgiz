using Ehgiz.DAL.Enums;

namespace Ehgiz.DAL.Entities;

public class Payment
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public EscrowStatus? EscrowStatus { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? StripePaymentIntentId { get; set; }

    public Booking Booking { get; set; } = null!;
}
