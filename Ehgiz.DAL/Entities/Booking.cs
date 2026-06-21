using Ehgiz.DAL.Enums;

namespace Ehgiz.DAL.Entities;

public class Booking
{
    public int Id { get; set; }
    public int ToolId { get; set; }
    public int RenterId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal RentalCost { get; set; }
    public decimal InsuranceAmount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal PricePerDay { get; set; }
    public BookingStatus? Status { get; set; }
    public string? AdminResolutionNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Tool Tool { get; set; } = null!;
    public ApplicationUser Renter { get; set; } = null!;
    public Payment? Payment { get; set; }
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<IssueReport> IssueReports { get; set; } = [];
    public ICollection<Handover> Handovers { get; set; } = [];
}
