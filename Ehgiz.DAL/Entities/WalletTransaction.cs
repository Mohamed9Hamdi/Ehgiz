using Ehgiz.DAL.Enums;

namespace Ehgiz.DAL.Entities;

public class WalletTransaction
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public decimal Amount { get; set; }
    public WalletTransactionType Type { get; set; }
    public string? Reference { get; set; }      // BookingId or StripePaymentIntentId
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public Wallet Wallet { get; set; } = null!;
}
