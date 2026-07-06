using Ehgiz.Application.DTOs.Payments;
using Ehgiz.DAL.Entities;
using Mapster;

namespace Ehgiz.Application.Mappings;

public class PaymentProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Payment, PaymentDto>()
            .Map(dest => dest.PaymentMethod, src => src.PaymentMethod == null ? null : src.PaymentMethod.Value.ToString())
            .Map(dest => dest.PaymentStatus, src => src.PaymentStatus == null ? null : src.PaymentStatus.Value.ToString())
            .Map(dest => dest.EscrowStatus, src => src.EscrowStatus == null ? null : src.EscrowStatus.Value.ToString());
    }
}
