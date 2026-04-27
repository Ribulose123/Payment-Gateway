using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentGate.Application.DTO;
using PaymentGate.Application.DTO.Paystack;
using PaymentGate.Application.Interface;
using PaymentGate.Domain.Entites;
using PaymentGate.Domain.Enums;
using PaymentGateway.Persistence;
using PaymentGateway.Persistence.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace PaymentGateway.Persistence.Services
{
    public class WithdrawalService:IWithdrawalServices
    {
        private readonly PaymentGatewayDbCOntext _context;
        private readonly IPaystackService _paystakeService;
        private readonly ILogger<WithdrawalService>  _logger;

        public WithdrawalService(PaymentGatewayDbCOntext context, IPaystackService paystackService, ILogger<WithdrawalService> logger)
        {
            _context = context;
            _paystakeService = paystackService;
            _logger = logger;

        }

        public async Task<WithdrawalResponseDto> WithdrawalAsync(WithdrawalRequestDto request)
        {
            if (request.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero");

            using var tr = await _context.Database.BeginTransactionAsync();
            Idempotency? idem = null;
            try
            {
                 idem = await _context.Idempotencies.FirstOrDefaultAsync(i => i.Key == request.IdempotencyKey
                && i.OperationType == IdempotencyOperationType.Withdrawal && i.ClientId == request.InitiatorId);

                if (idem != null)
                {
                    idem.ValidateRequestHash(request.RequsetHash);
                    idem.Touch();

                    if (idem.Status == IdempotencyStatus.Completed)
                        return JsonSerializer.Deserialize<WithdrawalResponseDto>(
                            idem.ResponseSnapshot!)!;

                    if (idem.Status == IdempotencyStatus.Failed)
                        throw new Exception("Previous transfer attempt failed.");

                    throw new Exception("Transfer is already being processed.");
                }

                idem = new Idempotency(
                    request.IdempotencyKey,
                    request.InitiatorId,
                    request.RequsetHash,
                    IdempotencyOperationType.FxTransfer,
                    TimeSpan.FromMinutes(10));

                await _context.Idempotencies.AddAsync(idem);
                await _context.SaveChangesAsync();

                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.WalletId == request.WalletId);

                if (wallet == null)
                    throw new Exception("Wallet not found");

                if (wallet.UserId != request.InitiatorId)
                    throw new UnauthorizedAccessException("you don't own this wallet");

                if (wallet.Balance < request.Amount)
                    throw new Exception("Insufficient balance.");

                var reference = $"WDR-{Guid.NewGuid().ToString()[..8]}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                wallet.Debit(request.Amount);

                var debitTx = new Transaction(
               walletId: wallet.WalletId,
               transferId: Guid.Empty,
               amount: request.Amount,
               currency: wallet.Currency,
               type: TransactionType.Debit,
               reference: reference);

                debitTx.MarkAsCompleted();
                _context.Transactions.Add(debitTx);

                var withdrawal = new WithdrawalRequest(
              walletId: wallet.WalletId,
              amount: request.Amount,
              reference: reference,
              recipientCode: request.RecipientCode);

                _context.WithdrawalRequests.Add(withdrawal);
                await _context.SaveChangesAsync();

                var transfer = await _paystakeService.InitiateTransferAsync(new InitiateTransferRequestDto
                {
                    Amount = request.Amount,
                    RecipientCode = request.RecipientCode,
                    Reference = reference,
                    Reason = request.Reason,
                    WalletId = wallet.WalletId,
                });

                withdrawal.AttachTransferCode(transfer.TransferCode);
                await _context.SaveChangesAsync();
                await tr.CommitAsync();


                return new WithdrawalResponseDto
                {
                    WithdrawalId = withdrawal.Id,
                    WalletId = wallet.WalletId,
                    Amount = request.Amount,
                    Reference = reference,
                    TransferCode = transfer.TransferCode,
                    Status = withdrawal.Status.ToString(),
                    CreatedAt = withdrawal.CreatedAt
                };
            }
            catch {
                await tr.RollbackAsync();
                throw;
            }
        }
    }
}
