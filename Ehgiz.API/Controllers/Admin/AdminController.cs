using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Admin;
using Ehgiz.Application.DTOs.Bookings;
using Ehgiz.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ehgiz.API.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    // ── Disputes ────────────────────────────────────────────────────────────

    // GET api/admin/disputes
    [HttpGet("disputes")]
    public async Task<IActionResult> GetDisputedBookings()
    {
        var result = await _adminService.GetDisputedBookingsAsync();
        return Ok(ApiResponse<IEnumerable<BookingDto>>.Success(result));
    }

    // GET api/admin/disputes/{bookingId}
    [HttpGet("disputes/{bookingId:int}")]
    public async Task<IActionResult> GetDisputeDetails(int bookingId)
    {
        var result = await _adminService.GetDisputeDetailsAsync(bookingId);
        return Ok(ApiResponse<DisputeDetailsDto>.Success(result));
    }

    // PUT api/admin/disputes/{bookingId}/favor-owner
    [HttpPut("disputes/{bookingId:int}/favor-owner")]
    public async Task<IActionResult> ResolveForOwner(int bookingId, [FromBody] ResolveDisputeRequest dto)
    {
        await _adminService.ResolveInFavorOfOwnerAsync(bookingId, dto);
        return Ok(ApiResponse<object>.Success(null!,
            $"Dispute for booking #{bookingId} resolved in favor of owner."));
    }

    // PUT api/admin/disputes/{bookingId}/favor-renter
    [HttpPut("disputes/{bookingId:int}/favor-renter")]
    public async Task<IActionResult> ResolveForRenter(int bookingId, [FromBody] ResolveDisputeRequest dto)
    {
        await _adminService.ResolveInFavorOfRenterAsync(bookingId, dto);
        return Ok(ApiResponse<object>.Success(null!,
            $"Dispute for booking #{bookingId} resolved in favor of renter."));
    }

    // PUT api/admin/disputes/{bookingId}/partial-refund
    [HttpPut("disputes/{bookingId:int}/partial-refund")]
    public async Task<IActionResult> PartialRefund(int bookingId, [FromBody] PartialRefundRequest dto)
    {
        await _adminService.ResolvePartialRefundAsync(bookingId, dto);
        return Ok(ApiResponse<object>.Success(null!,
            $"Dispute for booking #{bookingId} resolved with {dto.RefundPercentage}% partial refund."));
    }

    // PUT api/admin/disputes/{bookingId}/force-complete
    [HttpPut("disputes/{bookingId:int}/force-complete")]
    public async Task<IActionResult> ForceComplete(int bookingId, [FromBody] ResolveDisputeRequest dto)
    {
        await _adminService.ForceCompleteAsync(bookingId, dto);
        return Ok(ApiResponse<object>.Success(null!,
            $"Booking #{bookingId} force-completed with normal settlement."));
    }

    // PUT api/admin/disputes/{bookingId}/force-cancel
    [HttpPut("disputes/{bookingId:int}/force-cancel")]
    public async Task<IActionResult> ForceCancel(int bookingId, [FromBody] ResolveDisputeRequest dto)
    {
        await _adminService.ForceCancelAsync(bookingId, dto);
        return Ok(ApiResponse<object>.Success(null!,
            $"Booking #{bookingId} force-cancelled with full refund."));
    }

    // ── Issue Reports ───────────────────────────────────────────────────────

    // GET api/admin/issue-reports
    [HttpGet("issue-reports")]
    public async Task<IActionResult> GetIssueReports()
    {
        var result = await _adminService.GetIssueReportsAsync();
        return Ok(ApiResponse<IEnumerable<IssueReportDto>>.Success(result));
    }

    // GET api/admin/issue-reports/{id}
    [HttpGet("issue-reports/{id:int}")]
    public async Task<IActionResult> GetIssueReportById(int id)
    {
        var result = await _adminService.GetIssueReportByIdAsync(id);
        return Ok(ApiResponse<IssueReportDto>.Success(result));
    }

    // PUT api/admin/issue-reports/{id}/status
    [HttpPut("issue-reports/{id:int}/status")]
    public async Task<IActionResult> UpdateIssueReportStatus(int id, [FromBody] UpdateIssueStatusRequest dto)
    {
        await _adminService.UpdateIssueReportStatusAsync(id, dto);
        return Ok(ApiResponse<object>.Success(null!, $"Issue report #{id} status updated to '{dto.Status}'."));
    }

    // ── Platform Settings ───────────────────────────────────────────────────

    // GET api/admin/settings/platform-fee
    [HttpGet("settings/platform-fee")]
    public async Task<IActionResult> GetPlatformFee()
    {
        var fee = await _adminService.GetPlatformFeeAsync();
        return Ok(ApiResponse<object>.Success(new { feePercent = fee }));
    }

    // PUT api/admin/settings/platform-fee
    [HttpPut("settings/platform-fee")]
    public async Task<IActionResult> UpdatePlatformFee([FromBody] UpdatePlatformFeeRequest request)
    {
        await _adminService.UpdatePlatformFeeAsync(request.FeePercent);
        return Ok(ApiResponse<object>.Success(new { feePercent = request.FeePercent },
            "Platform fee updated successfully."));
    }
}
