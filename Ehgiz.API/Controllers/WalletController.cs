using System.Security.Claims;
using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Wallet;
using Ehgiz.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ehgiz.API.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);


    // GET api/wallet
    [HttpGet]
    public async Task<IActionResult> GetWallet()
    {
        var result = await _walletService.GetWalletAsync(CurrentUserId);
        return Ok(ApiResponse<WalletDto>.Success(result));
    }

    // GET api/wallet/transactions
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactionHistory()
    {
        var result = await _walletService.GetTransactionHistoryAsync(CurrentUserId);
        return Ok(ApiResponse<IEnumerable<WalletTransactionDto>>.Success(result));
    }

    // POST api/wallet/topup
    [HttpPost("topup")]
    public async Task<IActionResult> InitiateTopUp([FromBody] TopUpRequest dto)
    {
        var returnUrl = $"{Request.Scheme}://{Request.Host}/wallet/topup/return";

        var result = await _walletService.InitiateTopUpAsync(CurrentUserId, dto, returnUrl);
        return Ok(ApiResponse<TopUpResponse>.Success(result,
            "Checkout session created. Use the clientSecret with stripe.initEmbeddedCheckout() to render the payment form."));
    }

    // GET api/wallet/connect/onboard
    [HttpGet("connect/onboard")]
    public async Task<IActionResult> GetConnectOnboardingUrl()
    {
        var returnUrl = $"{Request.Scheme}://{Request.Host}/api/wallet/connect/return";
        var refreshUrl = $"{Request.Scheme}://{Request.Host}/api/wallet/connect/refresh";

        var result = await _walletService.GetConnectOnboardingUrlAsync(
            CurrentUserId, returnUrl, refreshUrl);

        return Ok(ApiResponse<ConnectOnboardingResponse>.Success(result,
            "Redirect the user to the OnboardingUrl to complete Stripe Connect setup."));
    }

    // GET api/wallet/connect/return  — Stripe redirects here after onboarding
    [HttpGet("connect/return")]
    public IActionResult ConnectReturn()
    {
        return Ok(ApiResponse<object>.Success(null!,
            "Stripe Connect onboarding completed. You can now withdraw funds."));
    }

    // GET api/wallet/connect/refresh  — Stripe redirects here if onboarding link expires
    [HttpGet("connect/refresh")]
    [AllowAnonymous]
    public IActionResult ConnectRefresh()
    {
        return Ok(ApiResponse<object>.Success(null!,
            "Onboarding link expired. Please request a new link from GET /api/wallet/connect/onboard."));
    }

    // POST api/wallet/withdraw
    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawalRequest dto)
    {
        await _walletService.WithdrawAsync(CurrentUserId, dto);
        return Ok(ApiResponse<object>.Success(null!,
            $"Withdrawal of {dto.Amount:C} initiated successfully."));
    }
}
