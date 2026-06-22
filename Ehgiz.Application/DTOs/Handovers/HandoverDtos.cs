using Microsoft.AspNetCore.Http;

namespace Ehgiz.Application.DTOs.Handovers;

public record SubmitHandoverRequest(string? Notes, List<IFormFile>? Images);

public record RespondHandoverRequest(bool Accept, string? Notes);

public record HandoverDto(
    int Id,
    int BookingId,
    string Type,
    string SubmittedByName,
    string? SubmitterNotes,
    DateTime SubmittedAt,
    string? RespondedByName,
    string? ResponderNotes,
    bool? IsAccepted,
    DateTime? RespondedAt,
    IEnumerable<HandoverImageDto>? Images);

public record HandoverImageDto(int Id, string ImageUrl, string? Caption);
