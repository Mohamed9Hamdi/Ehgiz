using Ehgiz.Application.DTOs.Bookings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ehgiz.Application.Interfaces;

public interface IBookingService
{
    Task<IEnumerable<BookingStatusDto>> GetBookingStatusesAsync();
    Task<IEnumerable<BookingIntervalDto>> GetBookingIntervalsAsync();
}
