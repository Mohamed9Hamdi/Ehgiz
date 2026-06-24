using Ehgiz.Application.DTOs.Wallet;

namespace Ehgiz.Application.Interfaces;

public interface IWalletService
{
    Task<WalletDto> GetWalletAsync(int userId);
    Task<TopUpResponse> InitiateTopUpAsync(int userId, TopUpRequest request, string returnUrl);
    Task CreditWalletFromStripeAsync(string sessionId, int userId, decimal amount);
    Task<IEnumerable<WalletTransactionDto>> GetTransactionHistoryAsync(int userId);
    Task<ConnectOnboardingResponse> GetConnectOnboardingUrlAsync(int userId, string returnUrl, string refreshUrl);
    Task WithdrawAsync(int userId, WithdrawalRequest request);
}

