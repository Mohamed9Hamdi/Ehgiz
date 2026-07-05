using System.Security.Claims;
using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Payments;
using Ehgiz.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // POST api/payments/webhook  — called by Stripe, no JWT auth
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        if (!Request.Headers.TryGetValue("Stripe-Signature", out var stripeSignature))
            return BadRequest("Missing Stripe-Signature header.");

        await _paymentService.HandleWebhookAsync(json, stripeSignature!);
        return Ok();
    }

    // GET api/payments/booking/{bookingId}
    [HttpGet("booking/{bookingId:int}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentByBooking(int bookingId)
    {
        var result = await _paymentService.GetPaymentByBookingAsync(bookingId, CurrentUserId);
        if (result is null)
            return NotFound(ApiResponse<PaymentDto>.Fail("No payment found for this booking."));

        return Ok(ApiResponse<PaymentDto>.Success(result));
    }
}
