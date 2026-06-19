using Ehgiz.DAL.Enums;

namespace Ehgiz.DAL.Entities;

public class Handover
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public HandoverType Type { get; set; }

    // Submitted by (Owner for Delivery, Renter for Return)
    public int SubmittedByUserId { get; set; }
    public string? SubmitterNotes { get; set; }
    public DateTime SubmittedAt { get; set; }

    // Responded by (Renter for Delivery, Owner for Return)
    public int? RespondedByUserId { get; set; }
    public string? ResponderNotes { get; set; }
    public bool? IsAccepted { get; set; }
    public DateTime? RespondedAt { get; set; }

    // Navigation
    public Booking Booking { get; set; } = null!;
    public ApplicationUser SubmittedByUser { get; set; } = null!;
    public ApplicationUser? RespondedByUser { get; set; }
    public ICollection<HandoverImage> Images { get; set; } = [];
}
