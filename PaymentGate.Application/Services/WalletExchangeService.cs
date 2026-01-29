using Microsoft.EntityFrameworkCore;
using PaymentGate.Application.Interface;
using PaymentGate.Domain.Entities;
using PaymentGate.Domain.ValueObjects;
using PaymentGateway.Persistence;

public class WalletExchangeService: IWalletExchangeService
{
    private readonly PaymentGatewayDbCOntext _db;
    private readonly IFxService _fx;
    private readonly IFeePolicy _feePolicy;

    public WalletExchangeService(
        PaymentGatewayDbCOntext db,
        IFxService fx,
        IFeePolicy feePolicy)
    {
        _db = db;
        _fx = fx;
        _feePolicy = feePolicy;
    }

    public async Task ExchangeAsync(
        Guid userId,
        Guid fromWalletId,
        Guid toWalletId,
        decimal amount)
    {
        using var tr = await _db.Database.BeginTransactionAsync();

        try
        {
            var from = await _db.Wallets.FirstOrDefaultAsync(w => w.WalletId == fromWalletId);
            var to = await _db.Wallets.FirstOrDefaultAsync(w => w.WalletId == toWalletId);

            if (from == null || to == null)
                throw new Exception("Wallet not found");

            if (from.UserId != userId || to.UserId != userId)
                throw new Exception("Wallet ownership mismatch");

            if(from.Currency == to.Currency)
                throw new Exception("Please use transfer for same currency exchange");

            //fx quote
            FxQuote quote = await _fx.QuoteAsync(from.Currency, to.Currency, amount);

            //fee calculation

            var feeResult = _feePolicy.Calculate(amount, from.Currency);

            if(from.Balance < feeResult.TotalDebit)
                throw new Exception("Insufficient balance");

            
            //Create Exchange

            var exchange = new FxExchange (
                userId,
                fromWalletId,
                toWalletId,
                amount,
                quote.ConvertedAmount,
                quote.Rate,
                feeResult.Fee,
                from.Currency,
                to.Currency);


        }
        catch (Exception)
        {
            await tr.RollbackAsync();
            throw;
        }
    }
}
