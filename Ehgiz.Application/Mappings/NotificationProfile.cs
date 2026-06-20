using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.DAL.Entities;
using Mapster;

namespace Ehgiz.Application.Mappings;

public class NotificationProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Notification, NotificationDto>()
            .Map(dest => dest.Type, src => src.Type.ToString());

        config.NewConfig<CreateNotificationDto, Notification>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.IsRead)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.User);
    }
}
