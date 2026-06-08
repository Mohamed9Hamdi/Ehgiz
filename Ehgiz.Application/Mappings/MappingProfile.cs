
using Ehgiz.DAL.Entities;
using Mapster;
using System;

namespace Ehgiz.Application.Mappings;

public class MappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // config.NewConfig<Booking, BookingStatusDto>()
        //     .Map(dest => dest.Price, src => src.TotalPrice);

        // config.NewConfig<Booking, BookingIntervalDto>()
        //     .Map(dest => dest.Price, src => src.TotalPrice)
        //     .Map(dest => dest.Interval, src => src.EndDate - src.StartDate);
    
    }
}
