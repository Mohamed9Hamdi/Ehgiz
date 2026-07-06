using Ehgiz.Application.DTOs.Admin;
using Ehgiz.Application.DTOs.Wallet;
using Ehgiz.DAL.Entities;
using Mapster;

namespace Ehgiz.Application.Mappings;

public class WalletProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Wallet, WalletDto>()
            .Map(dest => dest.TotalBalance, src => src.Balance + src.HeldBalance);

        config.NewConfig<WalletTransaction, WalletTransactionDto>()
            .Map(dest => dest.Type, src => src.Type.ToString());

        config.NewConfig<Wallet, AdminWalletDto>()
            .Map(dest => dest.UserFullName, src => src.User.FullName)
            .Map(dest => dest.UserEmail, src => src.User.Email ?? string.Empty);

        config.NewConfig<WalletTransaction, AdminWalletTransactionDto>()
            .Map(dest => dest.UserId, src => src.Wallet.UserId)
            .Map(dest => dest.UserFullName, src => src.Wallet.User.FullName)
            .Map(dest => dest.Type, src => src.Type.ToString());
    }
}
