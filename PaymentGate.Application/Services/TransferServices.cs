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
    public class TransferServices:ITransferInterface
    {
        public readonly PaymentGatewayDbCOntext _context;
        public readonly IFraudPolicy _fraudpolicy;

        public TransferServices(PaymentGatewayDbCOntext context, IFraudPolicy fraudpolicy)
        {
            _context = context;
            _fraudpolicy = fraudpolicy;
        }

        public async Task<TransferResponseDto> ExecuteTransferAsync(TransferRequestDto requestDto)
        {
            //Request Validation

            if (requestDto.Amount <= 0)
                throw new Exception("Amount must be greater zero(0)");

            if (requestDto.SourceWalletId == requestDto.DestinationWalletId)
                throw new Exception("Source and destination wallet cannot be the same");

            // idempotency

            var existingIdem = await _context.Idempotencies.FirstOrDefaultAsync(x => x.Key == requestDto.IdempotencyKey);

            if(existingIdem != null)
            {
                existingIdem.ValidateRequestHash(requestDto.RequsetHash);
                existingIdem.Touch();

                if(existingIdem.Status == IdempotencyStatus.Completed)
                    return JsonSerializer.Deserialize<TransferResponseDto>(existingIdem.ResponseSnapshot!);

                if (existingIdem.Status == IdempotencyStatus.Failed)
                    throw new Exception("Previous transfer attempt failed");

                throw new Exception("Transfer in process");
            }

            var idem = new Idempotency(
               requestDto.InitiatorId,
               requestDto.IdempotencyKey,
               requestDto.RequsetHash,
               IdempotencyOperationType.Transfer,
               TimeSpan.FromMinutes(10)
              );

            await _context.Idempotencies.AddAsync(idem);
            await _context.SaveChangesAsync();

            var sourceWallet = await _context.Wallets.FindAsync(requestDto.SourceWalletId);
            var destinationWallet = await _context.Wallets.FindAsync(requestDto.DestinationWalletId);


            if (sourceWallet == null)
                throw new Exception("Source wallet does not exist");

            if(destinationWallet == null)
                throw new Exception("Destination wallet does not exist");

            if(sourceWallet.Currency != requestDto.Currency || destinationWallet.Currency != requestDto.Currency)
                throw new Exception("Currency mismatch with wallet currency");

            if (sourceWallet.Balance < requestDto.Amount)
                throw new Exception("Insuffucent balance");

            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var transfer = new Transfer(
             sourceWallet.WalletId,
             destinationWallet.WalletId,
             requestDto.Amount,
             requestDto.Currency,
             requestDto.Description
            );

                await _context.Transfers.AddAsync(transfer);

                // run fraud check

                var fraudResult = _fraudpolicy.Evaluate(transfer, sourceWallet, destinationWallet);


                var fraudCheck = new FraudCheck(
                    transfer.TransferId,
                    FraudOperationType.Transfer,
                    fraudResult.RiskScore,
                    fraudResult.Decision,
                    fraudResult.Reason,
                    "FraudEngine"
                    );

                _context.FraudChecks.Add( fraudCheck );
                await _context.SaveChangesAsync();


                if (fraudResult.Decision == FraudDecision.Rejected)
                {
                    transfer.MarkFailed("Fraud rejected transfer");
                    await _context.SaveChangesAsync();
                    throw new Exception("Transfer rejected due to fraud risk");
                }

                if (fraudResult.Decision == FraudDecision.Review)
                {
                    transfer.MarkPendingReview("Awaiting manual fraud review");
                    await _context.SaveChangesAsync();
                    return TransferResponseDto.PendingReview(transfer.TransferId);
                }

            }
            catch { }

        }
    }
}
