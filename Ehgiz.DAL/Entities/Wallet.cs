namespace Ehgiz.DAL.Entities;

public class Wallet
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Balance { get; set; }        // available to spend / withdraw
    public decimal HeldBalance { get; set; }    // locked in active bookings
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ICollection<WalletTransaction> Transactions { get; set; } = [];
}
