using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.DTOs.Handovers;

namespace Ehgiz.Application.DTOs.Admin;

public record DisputeDetailsDto(
    BookingDto Booking,
    IEnumerable<IssueReportDto> Issues,
    IEnumerable<HandoverDto> Handovers);

public record IssueReportDto(
    int Id,
    string ReporterName,
    string? Title,
    string? Description,
    string Status,
    DateTime CreatedAt);

public record PartialRefundRequest(
    int RefundPercentage,
    string? ResolutionNotes);

public record ResolveDisputeRequest(string? ResolutionNotes);
