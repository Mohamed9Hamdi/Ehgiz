using Ehgiz.Application.Common;
using Ehgiz.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public SettingsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    // GET api/settings/platform-fee — public so the UI can show owners the platform commission
    [HttpGet("platform-fee")]
    public async Task<IActionResult> GetPlatformFee()
    {
        var fee = await _adminService.GetPlatformFeeAsync();
        return Ok(ApiResponse<object>.Success(new { feePercent = fee }));
    }
}
