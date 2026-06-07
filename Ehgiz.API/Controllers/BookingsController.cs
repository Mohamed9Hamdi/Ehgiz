using Ehgiz.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("statuses")]
    public async Task<IActionResult> GetStatuses()
    {
        var result = await _bookingService.GetBookingStatusesAsync();
        return Ok(result);
    }

    [HttpGet("intervals")]
    public async Task<IActionResult> GetIntervals()
    {
        var result = await _bookingService.GetBookingIntervalsAsync();
        return Ok(result);
    }
}
