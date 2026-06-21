using Ehgiz.Application.DTOs.Messages;
using Ehgiz.DAL.Entities;
using Mapster;

namespace Ehgiz.Application.Mappings;

public class MessageProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Message, MessageDto>()
            .Map(dest => dest.SenderName, src => src.Sender != null ? src.Sender.FullName : string.Empty)
            .Map(dest => dest.SenderAvatarUrl, src => src.Sender != null ? src.Sender.ProfileImageUrl : null)
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}
