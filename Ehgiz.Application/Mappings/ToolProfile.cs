using Ehgiz.Application.DTOs.Admin;
using Ehgiz.Application.DTOs.Tools;
using Ehgiz.DAL.Entities;
using Mapster;

namespace Ehgiz.Application.Mappings;


public class ToolProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
   
        config.NewConfig<Tool, ToolDto>()
            .Map(dest => dest.OwnerName, src => src.Owner.FullName)
            .Map(dest => dest.OwnerProfileImageUrl, src => src.Owner.ProfileImageUrl)
            .Map(dest => dest.CategoryName, src => src.Category.Name)
            .Map(dest => dest.Condition, src => src.Condition.HasValue ? src.Condition.Value.ToString() : null)
            .Map(dest => dest.ImageUrls, src => src.Images
                .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
                .Select(i => i.ImageUrl).ToList())
            .Map(dest => dest.Images, src => src.Images
                .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
                .Select(i => new ToolImageDto { Id = i.Id, ImageUrl = i.ImageUrl, IsPrimary = i.IsPrimary })
                .ToList());

       
        config.NewConfig<Tool, AdminListingDto>()
            .Map(dest => dest.CategoryName, src => src.Category.Name)
            .Map(dest => dest.OwnerName, src => src.Owner.FullName)
            .Map(dest => dest.FirstImageUrl, src => src.Images
                .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
                .Select(i => i.ImageUrl).FirstOrDefault())
            .Map(dest => dest.Condition, src => src.Condition.HasValue ? src.Condition.Value.ToString() : null);

        // TotalBookings is a separate aggregate injected by the caller.
        config.NewConfig<Tool, AdminListingDetailsDto>()
            .Map(dest => dest.CategoryName, src => src.Category.Name)
            .Map(dest => dest.OwnerName, src => src.Owner.FullName)
            .Map(dest => dest.ImageUrls, src => src.Images.Select(i => i.ImageUrl).ToList())
            .Map(dest => dest.Condition, src => src.Condition.HasValue ? src.Condition.Value.ToString() : null)
            .Ignore(dest => dest.TotalBookings);

        config.NewConfig<CreateToolDto, Tool>()
            .Map(dest => dest.IsAvailable, _ => true)              
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow)

            .Ignore(dest => dest.OwnerId)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Category)
            .Ignore(dest => dest.Images)
            .Ignore(dest => dest.Bookings)
            .Ignore(dest => dest.Id);


        config.NewConfig<UpdateToolDto, Tool>()
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow)
            .Ignore(dest => dest.OwnerId)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Category)
            .Ignore(dest => dest.Images)
            .Ignore(dest => dest.Bookings)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt);
    }
}