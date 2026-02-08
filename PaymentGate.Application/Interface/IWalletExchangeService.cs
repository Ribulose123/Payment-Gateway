

using PaymentGate.Application.DTO;

namespace PaymentGate.Application.Interface
{
    public interface IWalletExchangeService
    {
        Task <ExchangeResponseDto> ExchangeAsync(ExchangeServiceDto dto);
    }

}
