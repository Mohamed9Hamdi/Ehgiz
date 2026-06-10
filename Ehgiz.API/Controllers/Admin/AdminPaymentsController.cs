using Ehgiz.Application.Common;
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

    // PUT api/admin/disputes/{bookingId}/favor-owner
    [HttpPut("{bookingId:int}/favor-owner")]
    public async Task<IActionResult> ResolveForOwner(int bookingId)
    {
        await _adminPaymentService.ResolveDisputeInFavorOfOwnerAsync(bookingId);
        return Ok(ApiResponse<object>.Success(null!,
            $"Dispute for booking #{bookingId} resolved in favor of owner. Escrow released."));
    }

    // PUT api/admin/disputes/{bookingId}/favor-renter
    [HttpPut("{bookingId:int}/favor-renter")]
    public async Task<IActionResult> ResolveForRenter(int bookingId)
    {
        await _adminPaymentService.ResolveDisputeInFavorOfRenterAsync(bookingId);
        return Ok(ApiResponse<object>.Success(null!,
            $"Dispute for booking #{bookingId} resolved in favor of renter. Full refund issued."));
    }
}
