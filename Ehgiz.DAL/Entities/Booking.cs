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
    public BookingStatus? Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tool Tool { get; set; } = null!;
    public ApplicationUser Renter { get; set; } = null!;
    public Payment? Payment { get; set; }
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<IssueReport> IssueReports { get; set; } = [];
}
