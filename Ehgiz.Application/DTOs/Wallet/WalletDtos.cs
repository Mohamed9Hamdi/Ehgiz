namespace Ehgiz.Application.DTOs.Wallet;

public record WalletDto(
    int Id,
    decimal Balance,
    decimal HeldBalance,
    decimal TotalBalance);

public record TopUpRequest(
    decimal Amount,
    string Currency = "egp");

public record TopUpResponse(
    string ClientSecret,
    decimal Amount,
    string Currency);

public record WithdrawalRequest(decimal Amount);

public record WalletTransactionDto(
    int Id,
    decimal Amount,
    string Type,
    string? Description,
    string? Reference,
    DateTime CreatedAt);

public record ConnectOnboardingResponse(string OnboardingUrl);

public record MonthlyEarningsDto(
    string Month,
    decimal Gross,
    decimal Fees,
    decimal Net);
