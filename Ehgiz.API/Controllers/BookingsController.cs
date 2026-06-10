using System.Security.Claims;
using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // POST api/bookings
    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest dto)
    {
        var result = await _bookingService.CreateBookingAsync(CurrentUserId, dto);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<CreateBookingResponse>.Success(result, "Booking created successfully."));
    }

    // GET api/bookings/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookings()
    {
        var result = await _bookingService.GetMyBookingsAsync(CurrentUserId);
        return Ok(ApiResponse<IEnumerable<BookingDto>>.Success(result));
    }

    // GET api/bookings/received
    [HttpGet("received")]
    public async Task<IActionResult> GetReceivedBookings()
    {
        var result = await _bookingService.GetReceivedBookingsAsync(CurrentUserId);
        return Ok(ApiResponse<IEnumerable<BookingDto>>.Success(result));
    }

    // GET api/bookings/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBookingById(int id)
    {
        var result = await _bookingService.GetBookingByIdAsync(id, CurrentUserId);
        return Ok(ApiResponse<BookingDto>.Success(result));
    }

    // PUT api/bookings/{id}/cancel
    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        await _bookingService.CancelBookingAsync(id, CurrentUserId);
        return Ok(ApiResponse<object>.Success(null!, "Booking cancelled and refund issued to wallet."));
    }

    // PUT api/bookings/{id}/complete
    [HttpPut("{id:int}/complete")]
    public async Task<IActionResult> CompleteBooking(int id)
    {
        await _bookingService.CompleteBookingAsync(id, CurrentUserId);
        return Ok(ApiResponse<object>.Success(null!, "Booking completed. Earnings credited to owner wallet."));
    }
}
