

using PaymentGate.Domain.DTO;

namespace PaymentGate.Application.Interface
{
    public interface TransferInterface
    {
        Task<TransferResponseDto> ExecuteTransferAsync(TransferRequestDto requestDto); 
    }
}
