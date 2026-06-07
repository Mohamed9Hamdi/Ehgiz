using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Interfaces.Repositories;
using MapsterMapper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ehgiz.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IMapper _mapper;

    public BookingService(IBookingRepository bookingRepository, IMapper mapper)
    {
        _bookingRepository = bookingRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<BookingStatusDto>> GetBookingStatusesAsync()
    {
        var bookings = await _bookingRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<BookingStatusDto>>(bookings);
    }

    public async Task<IEnumerable<BookingIntervalDto>> GetBookingIntervalsAsync()
    {
        var bookings = await _bookingRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<BookingIntervalDto>>(bookings);
    }
}
