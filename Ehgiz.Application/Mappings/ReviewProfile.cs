using Ehgiz.Application.DTOs.Review;
using Ehgiz.DAL.Entities;
using Mapster;

namespace Ehgiz.Application.Mappings;

public class ReviewProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Review, ReviewDto>()
            .Map(dest => dest.ToolId, src => src.Booking.ToolId)
            .Map(dest => dest.ToolName, src => src.Booking.Tool.Name)
            .Map(dest => dest.RenterName, src => src.Booking.Renter.FullName);

        config.NewConfig<CreateReviewDto, Review>()
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Booking);
    }
}
