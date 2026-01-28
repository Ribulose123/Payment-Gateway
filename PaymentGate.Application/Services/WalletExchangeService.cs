using Microsoft.EntityFrameworkCore;
using PaymentGate.Application.Interface;
using PaymentGate.Domain.Entites;
using PaymentGate.Domain.Enums;
using PaymentGateway.Persistence;

namespace PaymentGate.Application.Services
{
    public class WalletExchangeService :IWalletExchangeService
    {
        private readonly PaymentGatewayDbCOntext _context;
        private readonly IFxService _fxService;

        public WalletExchangeService(PaymentGatewayDbCOntext context, IFxService fxService)
        {
            _context = context;
            _fxService = fxService;
        }

        public async Task ExchangeAsync(Guid userId, string fromCurrency, string toCurrency, decimal amount)
        {
            if (amount <= 0)
                throw new Exception("Amount can't be zero(0)");

            using var tx = await _context.Database.BeginTransactionAsync();

            var source = await _context.Wallets.FirstOrDefaultAsync(x => x.UserId == userId && x.Currency == fromCurrency);

            if (source == null)
                throw new Exception("Source Wallet not found");

            var destination = await _context.Wallets.FirstOrDefaultAsync(X => X.UserId == userId && X.Currency == toCurrency);

            if (destination == null)
                throw new Exception("Destination wallet not found");

            var qoute = await _fxService.QuoteAsync(fromCurrency, toCurrency, amount);

            source.Debit(amount);
            destination.Credit(qoute.ToAmount);

            var debitTx = new Transaction(
            source.WalletId,
            transferId: Guid.NewGuid(),
            amount,
            fromCurrency,
            TransactionType.Debit,
            "FX-DEBIT"
        );
            debitTx.MarkAsCompleted();

            var creditTx = new Transaction(
                destination.WalletId,
                transferId: debitTx.TransferId,
                qoute.ToAmount,
                toCurrency,
                TransactionType.Credit,
                "FX-CREDIT");

            creditTx.MarkAsCompleted();

            _context.Transactions.AddRange(debitTx, creditTx);
            await _context.SaveChangesAsync();

            await tx.CommitAsync();

        }
    }
}
