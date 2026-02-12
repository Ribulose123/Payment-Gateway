using Azure.Core;
using Microsoft.EntityFrameworkCore;
using PaymentGate.Application.DTO;
using PaymentGate.Application.Interface;
using PaymentGate.Domain.DTO;
using PaymentGate.Domain.Entites;
using PaymentGate.Domain.Entities;
using PaymentGate.Domain.Enums;
using PaymentGateway.Persistence;
using System.Text.Json;

public class WalletExchangeService: IWalletExchangeService
{
    private readonly PaymentGatewayDbCOntext _db;
    private readonly IFxService _fx;
    private readonly IFeePolicy _feePolicy;
    private readonly IFxFraudPolicy _fraudPolicy;

    public WalletExchangeService(
        PaymentGatewayDbCOntext db,
        IFxService fx,
        IFeePolicy feePolicy,
        IFxFraudPolicy fraudPolicy)
    {
        _db = db;
        _fx = fx;
        _feePolicy = feePolicy;
        _fraudPolicy = fraudPolicy;
    }

    public async Task<ExchangeResponseDto> ExchangeAsync(ExchangeServiceDto dto)
    {
        if (dto.Amount <= 0)
            throw new Exception("Amount must be greater than zero");

        if (dto.FromWalletId == dto.ToWalletId)
            throw new Exception("Source and destination wallet cannot be the same");

        Idempotency? idem = null;

        using var tr = await _db.Database.BeginTransactionAsync();

        try
        {
            // 🔁 Idempotency check
            idem = await _db.Idempotencies
               .FirstOrDefaultAsync(i =>
                   i.Key == dto.IdempotencyKey
                   && i.OperationType == IdempotencyOperationType.FxExchange
                   && i.ClientId == dto.InitiatorId
                   && i.ExpirationAt > DateTime.UtcNow);

            if (idem != null)
            {
                idem.ValidateRequestHash(dto.RequsetHash);
                idem.Touch();

                if (idem.Status == IdempotencyStatus.Completed)
                {
                    return JsonSerializer.Deserialize<ExchangeResponseDto>(
                        idem.ResponseSnapshot!)!;
                }

                if (idem.Status == IdempotencyStatus.Failed)
                    throw new Exception("Previous FX exchange failed");

                throw new Exception("FX exchange already in progress");
            }

            // Create idempotency record
            idem = new Idempotency(
                dto.InitiatorId,
                dto.IdempotencyKey,
                dto.RequsetHash,
                IdempotencyOperationType.FxExchange,
                TimeSpan.FromMinutes(10));

            _db.Idempotencies.Add(idem);
            await _db.SaveChangesAsync();

            // Load wallets
            var from = await _db.Wallets.FirstOrDefaultAsync(w => w.WalletId == dto.FromWalletId);
            var to = await _db.Wallets.FirstOrDefaultAsync(w => w.WalletId == dto.ToWalletId);

            if (from == null || to == null)
                throw new Exception("Wallet not found");

            if (from.UserId != dto.UserId || to.UserId != dto.UserId)
                throw new Exception("Wallet ownership mismatch");

            if (from.Currency == to.Currency)
                throw new Exception("Use transfer for same currency");

            // FX quote
            var quote = await _fx.QuoteAsync(from.Currency, to.Currency, dto.Amount);

            // Fee
            var fee = _feePolicy.Calculate(dto.Amount, from.Currency);

            if (from.Balance < fee.TotalDebit)
                throw new Exception("Insufficient balance");

            // Create FX Exchange
            var exchange = new FxExchange(
                dto.UserId,
                dto.FromWalletId,
                dto.ToWalletId,
                dto.Amount,
                quote.ConvertedAmount,
                quote.Rate,
                fee.Fee,
                from.Currency,
                to.Currency);

            _db.FxExchanges.Add(exchange);
            await _db.SaveChangesAsync();

            idem.AttachOperationReference(exchange.Id);

            // 🛡 Fraud check
            var fraudResult = _fraudPolicy.Evaluate(exchange, from, to);

            var fraudCheck = new FraudCheck(
                exchange.Id,
                FraudOperationType.FxExchange,
                fraudResult.RiskScore,
                fraudResult.Decision,
                fraudResult.Reason,
                "BasicFxFraudPolicy");

            _db.FraudChecks.Add(fraudCheck);
            await _db.SaveChangesAsync();

            if (fraudResult.Decision == FraudDecision.Rejected)
            {
                exchange.MarkFailed();
                idem.MarkAsFailed("FX exchange rejected by fraud");
                await _db.SaveChangesAsync();

                await tr.CommitAsync();
                throw new Exception("FX exchange blocked due to fraud");
            }

            // Transactions
            var debitTx = new Transaction(
                from.WalletId,
                exchange.Id,
                fee.TotalDebit,
                from.Currency,
                TransactionType.Debit,
                Guid.NewGuid().ToString());

            var creditTx = new Transaction(
                to.WalletId,
                exchange.Id,
                quote.ConvertedAmount,
                to.Currency,
                TransactionType.Credit,
                Guid.NewGuid().ToString());

            from.Debit(fee.TotalDebit);
            to.Credit(quote.ConvertedAmount);

            debitTx.MarkAsCompleted();
            creditTx.MarkAsCompleted();

            _db.Transactions.AddRange(debitTx, creditTx);

            exchange.MarkCompleted();

            // Build response
            var response = new ExchangeResponseDto
            {
                ExchangeId = exchange.Id,
                FromWalletId = from.WalletId,
                ToWalletId = to.WalletId,
                FromAmount = dto.Amount,
                ToAmount = quote.ConvertedAmount,
                Rate = quote.Rate,
                Fee = fee.Fee,
                Status = FxEchangeStatus.Success.ToString(),
            };

            idem.MarkAsCompleted(JsonSerializer.Serialize(response));

            await _db.SaveChangesAsync();
            await tr.CommitAsync();

            return response;
        }
        catch
        {
            await tr.RollbackAsync();

            if (idem != null && idem.Status == IdempotencyStatus.Processing)
            {
                idem.MarkAsFailed("System error during FX exchange");
                await _db.SaveChangesAsync();
            }

            throw;
        }
    }

}
