using Azure.Core;
using Microsoft.EntityFrameworkCore;
using PaymentGate.Application.DTO;
using PaymentGate.Application.Interface;
using PaymentGate.Domain.DTO;
using PaymentGate.Domain.Entites;
using PaymentGate.Domain.Entities;
using PaymentGate.Domain.Enums;
using PaymentGate.Domain.ValueObjects;
using PaymentGateway.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace PaymentGate.Application.Services
{
    public class FxTransfereServices : IFxTransfer
    {
        private readonly PaymentGatewayDbCOntext _context;
        private readonly IFraudPolicy _fraudPolicy;
        private readonly IFeePolicy _feePolicy;
        private readonly ILimitPolicy _limitPolicy;
        private readonly IFxService _fx;

        public FxTransfereServices(PaymentGatewayDbCOntext context, IFraudPolicy fraudPolicy, IFeePolicy feePolicy, ILimitPolicy limitPolicy, IFxService fx)
        {
            _context = context;
            _fraudPolicy = fraudPolicy;
            _feePolicy = feePolicy;
            _limitPolicy = limitPolicy;
            _fx = fx;
        }

        public async Task<FxTransferResponseDto> FxTransFereAsync(FxTransferRequestDto requestDto)
        {
            if (requestDto.Amount > 0)
                throw new ArgumentException("Amount must be greater than zero.");
            if (requestDto.FromWalletId == requestDto.ToWalletId)
                throw new ArgumentException("From and To wallet cannot be the same.");

            using var tr = await _context.Database.BeginTransactionAsync();
            Idempotency? idem;

            try
            {
                 idem = await _context.Idempotencies.FirstOrDefaultAsync(x => 
                x.Key == requestDto.IdempotencyKey &&  x.OperationType == IdempotencyOperationType.FxExchange
                   && x.ClientId == requestDto.InitiatorId
                   && x.ExpirationAt > DateTime.UtcNow);

                if (idem != null)
                {
                    idem.ValidateRequestHash(requestDto.RequsetHash);
                    idem.Touch();

                    if (idem.Status == IdempotencyStatus.Completed)
                        return JsonSerializer.Deserialize<FxTransferResponseDto>(
                            idem.ResponseSnapshot!)!;

                    if (idem.Status == IdempotencyStatus.Failed)
                        throw new Exception("Previous transfer failed");

                    throw new Exception("Transfer already processing");
                }

                idem = new Idempotency(
                    requestDto.IdempotencyKey,
                    requestDto.InitiatorId,
                    requestDto.RequsetHash,
                    IdempotencyOperationType.FxTransfer,
                    TimeSpan.FromMinutes(10));
                await _context.Idempotencies.AddAsync(idem);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FirstOrDefaultAsync(i => i.UserId == requestDto.InitiatorId);

                if(user == null)
                    throw new Exception("User not found");

                _limitPolicy.Validate(user, requestDto.Amount);

                //load users

                var source = await _context.Wallets
                    .FirstOrDefaultAsync(x => x.WalletId == requestDto.FromWalletId);

                var destination = await _context.Wallets
                    .FirstOrDefaultAsync(x => x.WalletId == requestDto.ToWalletId);

                if (source == null || destination == null)
                    throw new Exception("Wallet not found");
                var quote = await _fx.QuoteAsync(source.Currency, destination.Currency, requestDto.Amount);

                // Fee
                var fee = _feePolicy.Calculate(requestDto.Amount, source.Currency);

                if (source.Balance < fee.TotalDebit)
                    throw new Exception("Insufficient balance");

                //fx transfer
                var transfer = new FxTransfer(
                    source.WalletId,
                    destination.WalletId,
                    requestDto.Amount,
                    quote.ConvertedAmount,
                    quote.Rate,
                    fee.Fee,
                    source.Currency,
                    destination.Currency
                    );
                _context.FxTransfers.Add(transfer);
                await _context.SaveChangesAsync();

                idem.AttachOperationReference(transfer.Id);

                var fraudResult = _fraudPolicy.Evaluate(transfer, source, destination);

                  var typedFraudResult = fraudResult as FraudEvaluationResult ?? throw new InvalidOperationException("Fraud policy evaluation did not return expected result type.");
                var fraudCheck = new FraudCheck(
                    transfer.Id,
                    FraudOperationType.FxExchange,
                    typedFraudResult.RiskScore,
                    typedFraudResult.Decision,
                    typedFraudResult.Reason,
                    "BasicFxFraudPolicy");

                _context.FraudChecks.Add(fraudCheck);
                await _context.SaveChangesAsync();


                if (typedFraudResult.Decision == FraudDecision.Rejected)
                {
                    transfer.MarkAsFailed("Fraud rejected transfer");
                    idem.MarkAsFailed("Fraud rejection");

                    await _context.SaveChangesAsync();
                    await tr.CommitAsync();

                    throw new Exception("Transfer rejected due to fraud");
                }


                // 6️⃣ Debit source wallet + transaction
                source.Debit(fee.TotalDebit);

                var debitTx = new Transaction(
                    walletId: source.WalletId,
                    transferId: transfer.Id,
                    amount: fee.TotalDebit,
                    currency: requestDto.Currency,
                    type: TransactionType.Debit,
                    reference: Guid.NewGuid().ToString()
                );



                debitTx.MarkAsCompleted();


                // 7️⃣ Credit destination wallet + transaction
                destination.Credit(requestDto.Amount);

                var creditTx = new Transaction(
                    walletId: destination.WalletId,
                    transferId: transfer.Id,
                    amount: requestDto.Amount,
                    currency: requestDto.Currency,
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
                await tr.CommitAsync();

                var response = new FxTransferResponseDto
                {
                    FxTransferId= transfer.Id,
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
                throw;
            }
        }
    }
}