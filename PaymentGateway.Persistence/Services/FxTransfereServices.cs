using Microsoft.EntityFrameworkCore;
using PaymentGate.Application.DTO;
using PaymentGate.Application.Interface;
using PaymentGate.Domain.Entites;
using PaymentGate.Domain.Entities;
using PaymentGate.Domain.Enums;
using PaymentGate.Domain.ValueObjects;
using PaymentGateway.Persistence;
using System.Text.Json;

namespace PaymentGateway.Persistence.Services
{
    public class FxTransfereServices : IFxTransfer
    {
        private readonly PaymentGatewayDbCOntext _context;
        private readonly IFraudPolicy _fraudPolicy;
        private readonly IFeePolicy _feePolicy;
        private readonly ILimitPolicy _limitPolicy;
        private readonly IFxService _fx;

        public FxTransfereServices(
            PaymentGatewayDbCOntext context,
            IFraudPolicy fraudPolicy,
            IFeePolicy feePolicy,
            ILimitPolicy limitPolicy,
            IFxService fx)
        {
            _context = context;
            _fraudPolicy = fraudPolicy;
            _feePolicy = feePolicy;
            _limitPolicy = limitPolicy;
            _fx = fx;
        }

        public async Task<FxTransferResponseDto> FxTransFereAsync(FxTransferRequestDto requestDto)
        {
            if (requestDto.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            if (requestDto.FromWalletId == requestDto.ToWalletId)
                throw new ArgumentException("Source and destination wallets cannot be the same.");

            using var tr = await _context.Database.BeginTransactionAsync();
            Idempotency? idem = null;

            try
            {
                idem = await _context.Idempotencies.FirstOrDefaultAsync(x =>
                    x.Key == requestDto.IdempotencyKey
                    && x.OperationType == IdempotencyOperationType.FxTransfer
                    && x.ClientId == requestDto.InitiatorId
                    && x.ExpirationAt > DateTime.UtcNow);

                if (idem != null)
                {
                    idem.ValidateRequestHash(requestDto.RequestHash);
                    idem.Touch();

                    if (idem.Status == IdempotencyStatus.Completed)
                        return JsonSerializer.Deserialize<FxTransferResponseDto>(
                            idem.ResponseSnapshot!)!;

                    if (idem.Status == IdempotencyStatus.Failed)
                        throw new Exception("Previous transfer attempt failed.");

                    throw new Exception("Transfer is already being processed.");
                }

                // Create idempotency record
                idem = new Idempotency(
                    requestDto.IdempotencyKey,
                    requestDto.InitiatorId,
                    requestDto.RequestHash,
                    IdempotencyOperationType.FxTransfer,
                    TimeSpan.FromMinutes(10));

                await _context.Idempotencies.AddAsync(idem);
                await _context.SaveChangesAsync();

                // Load wallets
                var source = await _context.Wallets
                    .FirstOrDefaultAsync(x => x.WalletId == requestDto.FromWalletId);

                var destination = await _context.Wallets
                    .FirstOrDefaultAsync(x => x.WalletId == requestDto.ToWalletId);

                if (source == null || destination == null)
                    throw new Exception("One or both wallets were not found.");

                // Verify wallet ownership
                if (source.UserId != requestDto.InitiatorId)
                    throw new UnauthorizedAccessException("You do not have permission to transfer from this wallet.");

                // Load user for limit validation
                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.UserId == requestDto.InitiatorId);

                if (user == null)
                    throw new Exception("User not found.");

                _limitPolicy.Validate(user, requestDto.Amount);

                // FX quote — skip FX service for same currency
                FxQuote quote;
                if (source.Currency == destination.Currency)
                    quote = new FxQuote(rate: 1m, convertedAmount: requestDto.Amount);
                else
                    quote = await _fx.QuoteAsync(source.Currency, destination.Currency, requestDto.Amount);

                // Calculate fee
                var fee = _feePolicy.Calculate(requestDto.Amount, source.Currency);

                if (source.Balance < fee.TotalDebit)
                    throw new Exception("Insufficient balance to cover the transfer and fees.");

                // Create FX transfer record
                var transfer = new FxTransfer(
                    source.WalletId,
                    destination.WalletId,
                    requestDto.Amount,
                    quote.ConvertedAmount,
                    quote.Rate,
                    fee.Fee,
                    source.Currency,
                    destination.Currency);

                _context.FxTransfers.Add(transfer);
                await _context.SaveChangesAsync();

                idem.AttachOperationReference(transfer.Id);

                // Fraud evaluation
                var fraudResult = _fraudPolicy.Evaluate(transfer, source, destination);

                var typedFraudResult = fraudResult as FraudEvaluationResult
                    ?? throw new InvalidOperationException("Fraud policy did not return expected result type.");

                var fraudCheck = new FraudCheck(
                    transfer.Id,
                    FraudOperationType.FxTransfer,
                    typedFraudResult.RiskScore,
                    typedFraudResult.Decision,
                    typedFraudResult.Reason,
                    "BasicFxFraudPolicy");

                _context.FraudChecks.Add(fraudCheck);
                await _context.SaveChangesAsync();

                if (typedFraudResult.Decision == FraudDecision.Rejected)
                {
                    transfer.MarkAsFailed("Rejected by fraud policy.");
                    idem.MarkAsFailed("Fraud rejection.");
                    await _context.SaveChangesAsync();
                    await tr.CommitAsync();

                    return new FxTransferResponseDto
                    {
                        FxTransferId = transfer.Id,
                        FromWalletId = source.WalletId,
                        ToWalletId = destination.WalletId,
                        FromAmount = requestDto.Amount,
                        ToAmount = quote.ConvertedAmount,
                        Rate = quote.Rate,
                        Fee = fee.Fee,
                        Status = FxEchangeStatus.Failed.ToString(),
                        FailureReason = "Transfer rejected due to fraud policy."
                    };
                }

                // ? Debit source wallet
                source.Debit(fee.TotalDebit);

                var debitTx = new Transaction(
                    walletId: source.WalletId,
                    transferId: transfer.Id,
                    amount: fee.TotalDebit,
                    currency: source.Currency,
                    type: TransactionType.Debit,
                    reference: Guid.NewGuid().ToString());

                // ? Credit destination wallet
                destination.Credit(quote.ConvertedAmount);

                var creditTx = new Transaction(
                    walletId: destination.WalletId,
                    transferId: transfer.Id,
                    amount: quote.ConvertedAmount,
                    currency: destination.Currency,
                    type: TransactionType.Credit,
                    reference: Guid.NewGuid().ToString());

                debitTx.MarkAsCompleted();
                creditTx.MarkAsCompleted();

                _context.Transactions.AddRange(debitTx, creditTx);

                
                transfer.MarkSuccess(debitTx.Reference, creditTx.Reference);

                
                var response = new FxTransferResponseDto
                {
                    FxTransferId = transfer.Id,
                    FromWalletId = source.WalletId,
                    ToWalletId = destination.WalletId,
                    FromAmount = requestDto.Amount,
                    ToAmount = quote.ConvertedAmount,
                    Rate = quote.Rate,
                    Fee = fee.Fee,
                    Status = FxEchangeStatus.Success.ToString(),              
                };

                idem.MarkAsCompleted(JsonSerializer.Serialize(response));

                await _context.SaveChangesAsync();
                await tr.CommitAsync();

                return response;
            }
            catch
            {
                await tr.RollbackAsync();


                if (idem != null && idem.Status == IdempotencyStatus.Processing)
                {
                    idem.MarkAsFailed("System error during FX transfer.");
                    await _context.SaveChangesAsync();
                }

                throw;
            }
        }
    }
}
