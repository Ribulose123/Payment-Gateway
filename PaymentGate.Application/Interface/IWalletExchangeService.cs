

namespace PaymentGate.Application.Interface
{
    public interface IWalletExchangeService
    {
        Task ExchangeAsync(
            Guid userId,
            Guid fromWalletId,
            Guid toWalletId,
            decimal amount);
    }

}
