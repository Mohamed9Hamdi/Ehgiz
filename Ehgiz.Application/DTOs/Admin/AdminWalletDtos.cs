using Ehgiz.DAL.Enums;

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

public class AdminTransactionFilterDto
{
    public string? SearchTerm { get; set; }
    public WalletTransactionType? Type { get; set; }
    public int? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public record RollbackTransactionRequest(string Reason);

public record RollbackTransactionResultDto(
    int OriginalTransactionId,
    int SenderUserId,
    int ReceiverUserId,
    decimal Amount,
    string Reason,
    DateTime CreatedAt);
