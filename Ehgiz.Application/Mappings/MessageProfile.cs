using Ehgiz.Application.DTOs.Messages;
using Ehgiz.DAL.Entities;
using Mapster;

namespace Ehgiz.Application.Mappings;

public class MessageProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Message, MessageDto>()
            .Map(dest => dest.SenderName, src => src.Sender.FullName)
            .Map(dest => dest.SenderAvatarUrl, src => src.Sender.ProfileImageUrl)
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}
