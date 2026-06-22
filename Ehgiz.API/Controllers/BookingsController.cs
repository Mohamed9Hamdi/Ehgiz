using System.Security.Claims;
using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.DTOs.Handovers;
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
        return Ok(ApiResponse<IEnumerable<BookingCardDto>>.Success(result));
    }

    // GET api/bookings/received
    [HttpGet("received")]
    public async Task<IActionResult> GetReceivedBookings()
    {
        var result = await _bookingService.GetReceivedBookingsAsync(CurrentUserId);
        return Ok(ApiResponse<IEnumerable<BookingCardDto>>.Success(result));
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

    // ── Owner Accept/Reject ─────────────────────────────────────────────────

    // PUT api/bookings/{id}/accept
    [HttpPut("{id:int}/accept")]
    public async Task<IActionResult> AcceptBooking(int id)
    {
        await _bookingService.AcceptBookingAsync(id, CurrentUserId);
        return Ok(ApiResponse<object>.Success(null!, "Booking accepted."));
    }

    // PUT api/bookings/{id}/reject
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> RejectBooking(int id)
    {
        await _bookingService.RejectBookingAsync(id, CurrentUserId);
        return Ok(ApiResponse<object>.Success(null!, "Booking rejected. Refund issued to renter."));
    }

    // ── Delivery Handover ───────────────────────────────────────────────────

    // POST api/bookings/{id}/handover/delivery
    [HttpPost("{id:int}/handover/delivery")]
    public async Task<IActionResult> SubmitDeliveryHandover(int id, [FromForm] SubmitHandoverRequest dto)
    {
        await _bookingService.SubmitDeliveryHandoverAsync(id, CurrentUserId, dto);
        return Ok(ApiResponse<object>.Success(null!, "Delivery handover submitted. Waiting for renter confirmation."));
    }

    // PUT api/bookings/{id}/handover/delivery/respond
    [HttpPut("{id:int}/handover/delivery/respond")]
    public async Task<IActionResult> RespondDeliveryHandover(int id, [FromBody] RespondHandoverRequest dto)
    {
        await _bookingService.RespondDeliveryHandoverAsync(id, CurrentUserId, dto);
        var message = dto.Accept
            ? "Delivery accepted. Rental is now active."
            : "Delivery issue reported. Booking is now disputed.";
        return Ok(ApiResponse<object>.Success(null!, message));
    }

    // ── Return Handover ─────────────────────────────────────────────────────

    // POST api/bookings/{id}/handover/return
    [HttpPost("{id:int}/handover/return")]
    public async Task<IActionResult> SubmitReturnHandover(int id, [FromForm] SubmitHandoverRequest dto)
    {
        await _bookingService.SubmitReturnHandoverAsync(id, CurrentUserId, dto);
        return Ok(ApiResponse<object>.Success(null!, "Return handover submitted. Waiting for owner confirmation."));
    }

    // PUT api/bookings/{id}/handover/return/respond
    [HttpPut("{id:int}/handover/return/respond")]
    public async Task<IActionResult> RespondReturnHandover(int id, [FromBody] RespondHandoverRequest dto)
    {
        await _bookingService.RespondReturnHandoverAsync(id, CurrentUserId, dto);
        var message = dto.Accept
            ? "Return accepted. Booking completed. Earnings credited."
            : "Return issue reported. Booking is now disputed.";
        return Ok(ApiResponse<object>.Success(null!, message));
    }

    // ── Issue Reporting ─────────────────────────────────────────────────────

    // POST api/bookings/{id}/report-issue
    [HttpPost("{id:int}/report-issue")]
    public async Task<IActionResult> ReportIssue(int id, [FromBody] ReportIssueRequest dto)
    {
        await _bookingService.ReportIssueAsync(id, CurrentUserId, dto);
        return Ok(ApiResponse<object>.Success(null!, "Issue reported. Booking is now under dispute."));
    }

    // ── Calendar Availability ───────────────────────────────────────────────

    // GET api/bookings/tool/{toolId}/availability?year=2026&month=6
    [HttpGet("tool/{toolId:int}/availability")]
    [AllowAnonymous]
    public async Task<IActionResult> GetToolAvailability(int toolId, [FromQuery] int year, [FromQuery] int month)
    {
        if (year < 2020 || year > 2100)
            return BadRequest(ApiResponse<object>.Fail("Invalid year."));

        if (month < 1 || month > 12)
            return BadRequest(ApiResponse<object>.Fail("Invalid month."));

        var result = await _bookingService.GetToolAvailabilityAsync(toolId, year, month);
        return Ok(ApiResponse<ToolAvailabilityDto>.Success(result));
    }
}
