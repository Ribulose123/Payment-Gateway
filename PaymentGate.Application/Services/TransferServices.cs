using Microsoft.EntityFrameworkCore;
using PaymentGate.Application.Interface;
using PaymentGate.Domain.DTO;
using PaymentGate.Domain.Entites;
using PaymentGate.Domain.Entities;
using PaymentGate.Domain.Enums;
using PaymentGateway.Persistence;
using System.Text.Json;

namespace PaymentGate.Application.Services
{
    public class TransferServices : ITransferInterface
    {
        private readonly PaymentGatewayDbCOntext _context;
        private readonly IFraudPolicy _fraudPolicy;
        private readonly IFeePolicy _feePolicy;
        private readonly ILimitPolicy _limitPolicy;

        public TransferServices(
            PaymentGatewayDbCOntext context,
            IFraudPolicy fraudPolicy,
            IFeePolicy feePolicy,
            ILimitPolicy limitPolicy)
        {
            _context = context;
            _fraudPolicy = fraudPolicy;
            _feePolicy = feePolicy;
            _limitPolicy = limitPolicy;
        }

        public async Task<TransferResponseDto> ExecuteTransferAsync(TransferRequestDto request)
        {
          


            if (request.Amount <= 0)
                throw new Exception("Amount must be greater than zero");

            if (request.SourceWalletId == request.DestinationWalletId)
                throw new Exception("Source and destination wallet cannot be the same");

            Idempotency? idem;


            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1️⃣ Idempotency check
                idem = await _context.Idempotencies
                    .FirstOrDefaultAsync(x => x.Key == request.IdempotencyKey);

                if (idem != null)
                {
                    idem.ValidateRequestHash(request.RequsetHash);
                    idem.Touch();

                    if (idem.Status == IdempotencyStatus.Completed)
                        return JsonSerializer.Deserialize<TransferResponseDto>(
                            idem.ResponseSnapshot!)!;

                    if (idem.Status == IdempotencyStatus.Failed)
                        throw new Exception("Previous transfer failed");

                    throw new Exception("Transfer already processing");
                }

                // 2️⃣ Create idempotency
                idem = new Idempotency(
                    request.InitiatorId,
                    request.IdempotencyKey,
                    request.RequsetHash,
                    IdempotencyOperationType.Transfer,
                    TimeSpan.FromMinutes(10));

                _context.Idempotencies.Add(idem);
                await _context.SaveChangesAsync();


                var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == request.InitiatorId);

                if (user == null)
                    throw new Exception("User not found");

                _limitPolicy.Validate(user, request.Amount);

                // 3️⃣ Load wallets
                var source = await _context.Wallets
                    .FirstOrDefaultAsync(x => x.WalletId == request.SourceWalletId);

                var destination = await _context.Wallets
                    .FirstOrDefaultAsync(x => x.WalletId == request.DestinationWalletId);

                if (source == null || destination == null)
                    throw new Exception("Wallet not found");

                if (source.Currency != request.Currency ||
                    destination.Currency != request.Currency)
                    throw new Exception("Currency mismatch");

                var feeResult = _feePolicy.Calculate(request.Amount, request.Currency);

                if (source.Balance < feeResult.TotalDebit)
                    throw new Exception("Insufficient balance");

                // 4️⃣ Create transfer
                var transfer = new Transfer(
                    source.WalletId,
                    destination.WalletId,
                    request.Amount,
                    request.Currency,
                    feeResult.Fee,
                    request.Description);

                _context.Transfers.Add(transfer);
                await _context.SaveChangesAsync();

                idem.AttachOperationReference(transfer.TransferId);

                // 5️⃣ Fraud evaluation (decision stays OUTSIDE entity)
                var fraudResult = _fraudPolicy.Evaluate(
                    transfer, source, destination);

                var fraudCheck = new FraudCheck(
                    transfer.TransferId,
                    FraudOperationType.Transfer,
                    fraudResult.RiskScore,
                    fraudResult.Decision,
                    fraudResult.Reason,
                    "FraudEngine"
                );

                _context.FraudChecks.Add(fraudCheck);
                await _context.SaveChangesAsync();

                if (fraudResult.Decision == FraudDecision.Rejected)
                {
                    transfer.MarkFailed("Fraud rejected transfer");
                    idem.MarkAsFailed("Fraud rejection");

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    throw new Exception("Transfer rejected due to fraud");
                }

                // 6️⃣ Debit source wallet + transaction
                source.Debit(feeResult.TotalDebit);

                var debitTx = new Transaction(
                    walletId: source.WalletId,
                    transferId: transfer.TransferId,
                    amount: feeResult.TotalDebit,
                    currency: request.Currency,
                    type: TransactionType.Debit,
                    reference: Guid.NewGuid().ToString()
                );

                

                debitTx.MarkAsCompleted();

                // 7️⃣ Credit destination wallet + transaction
                destination.Credit(request.Amount);

                var creditTx = new Transaction(
                    walletId: destination.WalletId,
                    transferId: transfer.TransferId,
                    amount: request.Amount,
                    currency: request.Currency,
                    type: TransactionType.Credit,
                    reference: Guid.NewGuid().ToString()
                    );

               

                creditTx.MarkAsCompleted();

                _context.Transactions.AddRange(debitTx, creditTx);
                await _context.SaveChangesAsync();

                // 8️⃣ Mark transfer success
                transfer.MarkSuccess(
                    debitTx.Reference,
                    creditTx.Reference);

                await _context.SaveChangesAsync();

                // 9️⃣ Response + idempotency snapshot
                var response = new TransferResponseDto
                {
                    TransferId = transfer.TransferId,
                    Status = TransferStatus.Success.ToString(),
                    Amount = transfer.Amount,
                    Currency = transfer.Currency,
                    CreatedAt = DateTime.UtcNow
                };

                idem.MarkAsCompleted(JsonSerializer.Serialize(response));
                _limitPolicy.Consume(user, request.Amount);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                return response;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

    }
}
