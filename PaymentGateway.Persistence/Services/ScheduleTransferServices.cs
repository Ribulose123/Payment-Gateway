using Microsoft.EntityFrameworkCore;
using PaymentGate.Application.DTO;
using PaymentGate.Application.Interface;
using PaymentGate.Domain.Entites;
using PaymentGate.Domain.Enums;
using PaymentGateway.Persistence;
using System.Text.Json;

namespace PaymentGate.Application.Services
{
    public class ScheduleTransferServices : IScheduleTransfer
    {
        private readonly PaymentGatewayDbCOntext _content;
        private readonly IFraudPolicy _fraudPolicy;
        private readonly IFeePolicy _feePolicy;
        private readonly ILimitPolicy _limitPolicy;

        public ScheduleTransferServices(
            PaymentGatewayDbCOntext context,
            IFraudPolicy fraudPolicy,
            IFeePolicy feePolicy,
            ILimitPolicy limitPolicy)
        {
            _content = context;
            _fraudPolicy = fraudPolicy;
            _feePolicy = feePolicy;
            _limitPolicy = limitPolicy;
        }

        public async Task<ScheduledTransferResponseDto> ScheduledTransferAsync(
            ScheduledTransferRequestDto requestDto)
        {
            if (requestDto.Amount <= 0)
                throw new Exception("Amount must be greater than zero.");

            if (requestDto.FromWalletId == requestDto.ToWalletId)
                throw new Exception("Source and destination wallet must not be the same.");

            Idempotency? idem;
            using var tr = await _content.Database.BeginTransactionAsync();

            try
            {
                idem = await _content.Idempotencies.FirstOrDefaultAsync(x =>
                    x.Key == requestDto.IdempotencyKey &&
                    x.OperationType == IdempotencyOperationType.ScheduledTransfer &&
                    x.ClientId == requestDto.InitiatorId &&
                    x.ExpirationAt > DateTime.UtcNow);

                if (idem != null)
                {
                    idem.ValidateRequestHash(requestDto.RequestHash);
                    idem.Touch();

                    if (idem.Status == IdempotencyStatus.Completed)
                        return JsonSerializer.Deserialize<ScheduledTransferResponseDto>(
                            idem.ResponseSnapshot!)!;

                    if (idem.Status == IdempotencyStatus.Failed)
                        throw new Exception("Previous transfer failed.");

                    throw new Exception("Transfer already processing.");
                }

                idem = new Idempotency(
                    requestDto.InitiatorId,
                    requestDto.IdempotencyKey,
                    requestDto.RequestHash,
                    IdempotencyOperationType.ScheduledTransfer,
                    TimeSpan.FromMinutes(10));

                _content.Idempotencies.Add(idem);
                await _content.SaveChangesAsync();

                // Load user
                var user = await _content.Users
                    .FirstOrDefaultAsync(x => x.UserId == requestDto.InitiatorId);

                if (user == null)
                    throw new Exception("User not found.");

                _limitPolicy.Validate(user, requestDto.Amount);

                // Load wallets
                var source = await _content.Wallets
                    .FirstOrDefaultAsync(x => x.WalletId == requestDto.FromWalletId);

                var destination = await _content.Wallets
                    .FirstOrDefaultAsync(x => x.WalletId == requestDto.ToWalletId);

                if (source == null || destination == null)
                    throw new Exception("One or both wallets were not found.");

                // Verify ownership
                if (source.UserId != requestDto.InitiatorId)
                    throw new UnauthorizedAccessException("You do not own the source wallet.");
                if(source.Currency != requestDto.Currency || destination.Currency != requestDto.Currency)
                    throw new Exception("Currency mismatch with wallets.");

                // Calculate fee
                var feeResult = _feePolicy.Calculate(requestDto.Amount, requestDto.Currency);

                if (source.Balance < feeResult.TotalDebit)
                    throw new Exception("Insufficient balance.");

                // Create scheduled transfer
                var transfer = new ScheduledTransfer(
                    initiatorId: requestDto.InitiatorId,
                    fromWallet: source.WalletId,
                    toWallet: destination.WalletId,
                    amount: requestDto.Amount,
                    currency: requestDto.Currency,
                    fee: feeResult.Fee,
                    scheduleAt: requestDto.ScheduleAt,
                    isRecurring: requestDto.IsRecurring,
                    recurrenceInterval: requestDto.RecurrenceInterval);

                _content.ScheduledTransfers.Add(transfer);
                await _content.SaveChangesAsync();

                idem.AttachOperationReference(transfer.Id);

                var response = MapToResponse(transfer);

                idem.MarkAsCompleted(JsonSerializer.Serialize(response));
                await _content.SaveChangesAsync();
                await tr.CommitAsync();

                return response;
            }
            catch
            {
                await tr.RollbackAsync();
                throw;
            }
        }

       
        public async Task<IEnumerable<ScheduledTransferResponseDto>> GetAllAsync(Guid initiatorId)
        {
            var transfers = await _content.ScheduledTransfers
                .Where(x => x.InitiatorId == initiatorId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return transfers.Select(t => MapToResponse(t));
        }

       
        public async Task<ScheduledTransferResponseDto?> GetByIdAsync(
            Guid scheduleTransferId,
            Guid initiatorId)
        {
            var transfer = await _content.ScheduledTransfers
                .FirstOrDefaultAsync(x =>
                    x.Id == scheduleTransferId &&
                    x.InitiatorId == initiatorId);

            if (transfer == null)
                return null;

            return MapToResponse(transfer);
        }

        // ✅ NEW: Cancel a scheduled transfer
        public async Task CancelAsync(Guid scheduleTransferId, Guid initiatorId)
        {
            var transfer = await _content.ScheduledTransfers
                .FirstOrDefaultAsync(x =>
                    x.Id == scheduleTransferId &&
                    x.InitiatorId == initiatorId);

            if (transfer == null)
                throw new Exception("Scheduled transfer not found.");

            transfer.Cancel();
            await _content.SaveChangesAsync();
        }

        
        private static ScheduledTransferResponseDto MapToResponse(ScheduledTransfer transfer)
        {
            return new ScheduledTransferResponseDto
            {
                ScheduleTransferId = transfer.Id,
                FromWalletId = transfer.FromWallet,
                ToWalletId = transfer.ToWallet,
                Amount = transfer.Amount,
                Fee = transfer.Fee,
                TotalAmount = transfer.TotalAmount,
                Currency = transfer.Currency,
                Status = transfer.TransferStatus.ToString(),
                ScheduleAt = transfer.ScheduleAt,
                IsRecurring = transfer.IsRecurring,
                RecurrenceInterval = transfer.IsRecurring
                    ? transfer.RecurrenceInterval.ToString()
                    : null,
                NextRunAt = transfer.NextRunAt,
                LastRunAt = transfer.LastRunAt == default ? null : transfer.LastRunAt,
                FailureReason = transfer.FailureReason,
                DebitTransactionReference = transfer.DebitTransactionReference,
                CreditTransactionReference = transfer.CreditTransactionReference,
                CreatedAt = transfer.CreatedAt
            };
        }
    }
}