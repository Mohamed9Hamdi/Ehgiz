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

    // ── Dashboard ────────────────────────────────────────────────────────────

    // GET api/admin/dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var result = await _adminService.GetDashboardStatsAsync();
        return Ok(ApiResponse<AdminDashboardStatsDto>.Success(result));
    }

    // ── Disputes ─────────────────────────────────────────────────────────────

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
        return Ok(ApiResponse<object>.Success(null!, $"Dispute for booking #{bookingId} resolved in favor of owner."));
    }

    // PUT api/admin/disputes/{bookingId}/favor-renter
    [HttpPut("disputes/{bookingId:int}/favor-renter")]
    public async Task<IActionResult> ResolveForRenter(int bookingId, [FromBody] ResolveDisputeRequest dto)
    {
        await _adminService.ResolveInFavorOfRenterAsync(bookingId, dto);
        return Ok(ApiResponse<object>.Success(null!, $"Dispute for booking #{bookingId} resolved in favor of renter."));
    }

    // PUT api/admin/disputes/{bookingId}/partial-refund
    [HttpPut("disputes/{bookingId:int}/partial-refund")]
    public async Task<IActionResult> PartialRefund(int bookingId, [FromBody] PartialRefundRequest dto)
    {
        await _adminService.ResolvePartialRefundAsync(bookingId, dto);
        return Ok(ApiResponse<object>.Success(null!, $"Dispute for booking #{bookingId} resolved with {dto.RefundPercentage}% partial refund."));
    }

    // PUT api/admin/disputes/{bookingId}/force-complete
    [HttpPut("disputes/{bookingId:int}/force-complete")]
    public async Task<IActionResult> ForceComplete(int bookingId, [FromBody] ResolveDisputeRequest dto)
    {
        await _adminService.ForceCompleteAsync(bookingId, dto);
        return Ok(ApiResponse<object>.Success(null!, $"Booking #{bookingId} force-completed with normal settlement."));
    }

    // PUT api/admin/disputes/{bookingId}/force-cancel
    [HttpPut("disputes/{bookingId:int}/force-cancel")]
    public async Task<IActionResult> ForceCancel(int bookingId, [FromBody] ResolveDisputeRequest dto)
    {
        await _adminService.ForceCancelAsync(bookingId, dto);
        return Ok(ApiResponse<object>.Success(null!, $"Booking #{bookingId} force-cancelled with full refund."));
    }

    // ── Issue Reports ─────────────────────────────────────────────────────────

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

    // ── User Management ───────────────────────────────────────────────────────

    // GET api/admin/users
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _adminService.GetUsersAsync();
        return Ok(ApiResponse<IEnumerable<AdminUserDetailsDto>>.Success(result));
    }

    // GET api/admin/users/{id}
    [HttpGet("users/{id:int}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var result = await _adminService.GetUserByIdAsync(id);
        return Ok(ApiResponse<AdminUserDetailsDto>.Success(result));
    }

    // PUT api/admin/users/{id}/active
    [HttpPut("users/{id:int}/active")]
    public async Task<IActionResult> SetUserActive(int id, [FromBody] SetUserActiveRequest dto)
    {
        await _adminService.SetUserActiveAsync(id, dto.IsActive);
        var status = dto.IsActive ? "activated" : "deactivated";
        return Ok(ApiResponse<object>.Success(null!, $"User #{id} has been {status}."));
    }

    // PUT api/admin/users/{id}/role
    [HttpPut("users/{id:int}/role")]
    public async Task<IActionResult> SetUserRole(int id, [FromBody] SetUserRoleRequest dto)
    {
        await _adminService.SetUserRoleAsync(id, dto.Role);
        return Ok(ApiResponse<object>.Success(null!, $"User #{id} role updated to '{dto.Role}'."));
    }

    // DELETE api/admin/users/{id}
    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _adminService.DeleteUserAsync(id);
        return Ok(ApiResponse<object>.Success(null!, $"User #{id} has been deleted."));
    }

    // ── Listing Management ────────────────────────────────────────────────────

    // GET api/admin/listings
    [HttpGet("listings")]
    public async Task<IActionResult> GetListings()
    {
        var result = await _adminService.GetListingsAsync();
        return Ok(ApiResponse<IEnumerable<AdminListingDto>>.Success(result));
    }

    // GET api/admin/listings/{id}
    [HttpGet("listings/{id:int}")]
    public async Task<IActionResult> GetListingById(int id)
    {
        var result = await _adminService.GetListingByIdAsync(id);
        return Ok(ApiResponse<AdminListingDetailsDto>.Success(result));
    }

    // PUT api/admin/listings/{id}/availability
    [HttpPut("listings/{id:int}/availability")]
    public async Task<IActionResult> SetListingAvailability(int id, [FromBody] SetListingAvailabilityRequest dto)
    {
        await _adminService.SetListingAvailabilityAsync(id, dto.IsAvailable);
        var status = dto.IsAvailable ? "enabled" : "disabled";
        return Ok(ApiResponse<object>.Success(null!, $"Listing #{id} availability {status}."));
    }

    // DELETE api/admin/listings/{id}
    [HttpDelete("listings/{id:int}")]
    public async Task<IActionResult> DeleteListing(int id)
    {
        await _adminService.DeleteListingAsync(id);
        return Ok(ApiResponse<object>.Success(null!, $"Listing #{id} has been deleted."));
    }

    // ── Category Management ───────────────────────────────────────────────────

    // GET api/admin/categories
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _adminService.GetCategoriesAsync();
        return Ok(ApiResponse<IEnumerable<AdminCategoryDto>>.Success(result));
    }

    // POST api/admin/categories
    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest dto)
    {
        var result = await _adminService.CreateCategoryAsync(dto);
        return CreatedAtAction(nameof(GetCategories), ApiResponse<AdminCategoryDto>.Success(result, "Category created."));
    }

    // PUT api/admin/categories/{id}
    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest dto)
    {
        var result = await _adminService.UpdateCategoryAsync(id, dto);
        return Ok(ApiResponse<AdminCategoryDto>.Success(result, "Category updated."));
    }

    // DELETE api/admin/categories/{id}
    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await _adminService.DeleteCategoryAsync(id);
        return Ok(ApiResponse<object>.Success(null!, $"Category #{id} has been deleted."));
    }

    // ── Wallet & Transaction Management ───────────────────────────────────────

    // GET api/admin/wallets
    [HttpGet("wallets")]
    public async Task<IActionResult> GetWallets()
    {
        var result = await _adminService.GetWalletsAsync();
        return Ok(ApiResponse<IEnumerable<AdminWalletDto>>.Success(result));
    }

    // GET api/admin/transactions?email=&type=&page=&pageSize=
    [HttpGet("transactions")]
    public async Task<IActionResult> SearchTransactions([FromQuery] AdminTransactionFilterDto filter)
    {
        var result = await _adminService.SearchTransactionsAsync(filter);
        return Ok(ApiResponse<PagedResult<AdminWalletTransactionDto>>.Success(result));
    }

    // POST api/admin/transactions/{id}/rollback
    [HttpPost("transactions/{id:int}/rollback")]
    public async Task<IActionResult> RollbackTransaction(int id, [FromBody] RollbackTransactionRequest dto)
    {
        var result = await _adminService.RollbackTransactionAsync(id, dto);
        return Ok(ApiResponse<RollbackTransactionResultDto>.Success(result, $"Transaction #{id} has been rolled back."));
    }

    // ── Platform Settings ─────────────────────────────────────────────────────

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
        return Ok(ApiResponse<object>.Success(new { feePercent = request.FeePercent }, "Platform fee updated successfully."));
    }
}
