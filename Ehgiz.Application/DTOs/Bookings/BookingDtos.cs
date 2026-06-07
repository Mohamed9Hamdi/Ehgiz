using Ehgiz.DAL.Enums;
using System;

namespace Ehgiz.Application.DTOs.Bookings;

public class BookingStatusDto
{
    public decimal Price { get; set; }
    public BookingStatus? Status { get; set; }
}

public class BookingIntervalDto
{
    public decimal Price { get; set; }
    public TimeSpan Interval { get; set; }
}
