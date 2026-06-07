using Ehgiz.DAL.Enums;

namespace Ehgiz.DAL.Entities;

public class IssueReport
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int ReporterId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public IssueReportStatus? Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public Booking Booking { get; set; } = null!;
    public User Reporter { get; set; } = null!;
}
