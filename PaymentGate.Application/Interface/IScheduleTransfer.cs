

using PaymentGate.Application.DTO;

namespace PaymentGate.Application.Interface
{
    public interface IScheduleTransfer
    {
        Task<ScheduledTransferResponseDto> ScheduledTransferAsync(ScheduledTransferRequestDto requestDto);
        Task<IEnumerable<ScheduledTransferResponseDto>> GetAllAsync(Guid initiatorId);
        Task<ScheduledTransferResponseDto?> GetByIdAsync(Guid scheduleTransferId, Guid initiatorId);
        Task CancelAsync(Guid scheduleTransferId, Guid initiatorId);
    }
}
