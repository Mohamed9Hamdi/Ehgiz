using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Admin;
using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ehgiz.API.Controllers.Admin;

[ApiController]
[Route("api/admin/disputes")]
[Authorize(Roles = "Admin")]
public class AdminPaymentsController : ControllerBase
{
    private readonly IAdminPaymentService _adminPaymentService;

    public AdminPaymentsController(IAdminPaymentService adminPaymentService)
    {
        _adminPaymentService = adminPaymentService;
    }

    // GET api/admin/disputes
    [HttpGet]
    public async Task<IActionResult> GetDisputedBookings()
    {
        var result = await _adminPaymentService.GetDisputedBookingsAsync();
        return Ok(ApiResponse<IEnumerable<BookingDto>>.Success(result));
    }

    // GET api/admin/disputes/{bookingId}
    [HttpGet("{bookingId:int}")]
    public async Task<IActionResult> GetDisputeDetails(int bookingId)
    {
        var result = await _adminPaymentService.GetDisputeDetailsAsync(bookingId);
        return Ok(ApiResponse<DisputeDetailsDto>.Success(result));
    }

    // PUT api/admin/disputes/{bookingId}/favor-owner
    [HttpPut("{bookingId:int}/favor-owner")]
    public async Task<IActionResult> ResolveForOwner(int bookingId, [FromBody] ResolveDisputeRequest dto)
    {
        await _adminPaymentService.ResolveInFavorOfOwnerAsync(bookingId, dto);
        return Ok(ApiResponse<object>.Success(null!,
            $"Dispute for booking #{bookingId} resolved in favor of owner."));
    }

    // PUT api/admin/disputes/{bookingId}/favor-renter
    [HttpPut("{bookingId:int}/favor-renter")]
    public async Task<IActionResult> ResolveForRenter(int bookingId, [FromBody] ResolveDisputeRequest dto)
    {
        await _adminPaymentService.ResolveInFavorOfRenterAsync(bookingId, dto);
        return Ok(ApiResponse<object>.Success(null!,
            $"Dispute for booking #{bookingId} resolved in favor of renter."));
    }

    // PUT api/admin/disputes/{bookingId}/partial-refund
    [HttpPut("{bookingId:int}/partial-refund")]
    public async Task<IActionResult> PartialRefund(int bookingId, [FromBody] PartialRefundRequest dto)
    {
        await _adminPaymentService.ResolvePartialRefundAsync(bookingId, dto);
        return Ok(ApiResponse<object>.Success(null!,
            $"Dispute for booking #{bookingId} resolved with {dto.RefundPercentage}% partial refund."));
    }

    // PUT api/admin/disputes/{bookingId}/force-complete
    [HttpPut("{bookingId:int}/force-complete")]
    public async Task<IActionResult> ForceComplete(int bookingId, [FromBody] ResolveDisputeRequest dto)
    {
        await _adminPaymentService.ForceCompleteAsync(bookingId, dto);
        return Ok(ApiResponse<object>.Success(null!,
            $"Booking #{bookingId} force-completed with normal settlement."));
    }

    // PUT api/admin/disputes/{bookingId}/force-cancel
    [HttpPut("{bookingId:int}/force-cancel")]
    public async Task<IActionResult> ForceCancel(int bookingId, [FromBody] ResolveDisputeRequest dto)
    {
        await _adminPaymentService.ForceCancelAsync(bookingId, dto);
        return Ok(ApiResponse<object>.Success(null!,
            $"Booking #{bookingId} force-cancelled with full refund."));
    }
}
