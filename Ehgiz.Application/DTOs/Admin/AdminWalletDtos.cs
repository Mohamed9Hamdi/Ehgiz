namespace Ehgiz.Application.DTOs.Admin;

public record AdminWalletDto(
    int Id,
    int UserId,
    string UserFullName,
    string UserEmail,
    decimal Balance,
    decimal HeldBalance,
    DateTime UpdatedAt);

public record AdminWalletTransactionDto(
    int Id,
    int WalletId,
    int UserId,
    string UserFullName,
    decimal Amount,
    string Type,
    string? Description,
    string? Reference,
    DateTime CreatedAt);
