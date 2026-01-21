

using PaymentGate.Domain.DTO;

namespace PaymentGate.Application.Interface
{
    public interface ITransferInterface
    {
        Task<TransferResponseDto> ExecuteTransferAsync(TransferRequestDto requestDto); 
    }
}
