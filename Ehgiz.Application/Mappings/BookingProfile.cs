using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.DTOs.Handovers;
using Ehgiz.DAL.Entities;
using Mapster;

namespace Ehgiz.Application.Mappings;

public class BookingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<HandoverImage, HandoverImageDto>();

        config.NewConfig<Handover, HandoverDto>()
            .Map(dest => dest.Type, src => src.Type.ToString())
            .Map(dest => dest.SubmittedByName, src => src.SubmittedByUser.FullName ?? string.Empty)
            .Map(dest => dest.RespondedByName, src => src.RespondedByUser.FullName);

        config.NewConfig<Booking, BookingDto>()
            .Map(dest => dest.ToolName, src => src.Tool.Name ?? string.Empty)
            .Map(dest => dest.ToolImageUrl, src => src.Tool.Images.Select(i => i.ImageUrl).FirstOrDefault())
            .Map(dest => dest.OwnerId, src => src.Tool.OwnerId)
            .Map(dest => dest.OwnerName, src => src.Tool.Owner.FullName ?? string.Empty)
            .Map(dest => dest.OwnerProfileImageUrl, src => src.Tool.Owner.ProfileImageUrl)
            .Map(dest => dest.RenterName, src => src.Renter.FullName ?? string.Empty)
            .Map(dest => dest.RenterProfileImageUrl, src => src.Renter.ProfileImageUrl)
            .Map(dest => dest.Days, src => (int)(src.EndDate.Date - src.StartDate.Date).TotalDays)
            .Map(dest => dest.InsurancePrice, src => src.InsuranceAmount)
            .Map(dest => dest.Status, src => src.Status == null ? string.Empty : src.Status.Value.ToString())
            .Map(dest => dest.PaymentStatus, src => src.Payment == null || src.Payment.PaymentStatus == null
                ? null
                : src.Payment.PaymentStatus.Value.ToString())
            .Map(dest => dest.EscrowStatus, src => src.Payment == null || src.Payment.EscrowStatus == null
                ? null
                : src.Payment.EscrowStatus.Value.ToString())
            .Map(dest => dest.HasReview, src => src.Reviews.Any())
            // Server-computed per-user actions are injected by the caller; default to empty.
            .Map(dest => dest.AllowedActions, _ => (IReadOnlyList<string>)Array.Empty<string>());

        config.NewConfig<Booking, BookedDateRange>()
            .Map(dest => dest.BookingId, src => src.Id)
            .Map(dest => dest.Status, src => src.Status == null ? string.Empty : src.Status.Value.ToString());
    }
}
