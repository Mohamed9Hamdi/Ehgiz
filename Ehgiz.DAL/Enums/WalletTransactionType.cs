namespace Ehgiz.DAL.Enums;

public enum WalletTransactionType
{
    TopUp           = 1,
    BookingDebit    = 2,
    EarningCredit   = 3,
    InsuranceRefund = 4,
    BookingRefund   = 5,
    Withdrawal      = 6,
    LateFeeDebit    = 7,
    LateFeeCredit   = 8,
    PartialRefund   = 9,
    DisputeCredit   = 10
}
