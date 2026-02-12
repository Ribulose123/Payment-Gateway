using PaymentGate.Domain.ValueObjects;

namespace PaymentGate.Application.Interface
{
    public interface IFxService
    {
        Task<FxQuote> QuoteAsync(string from, string to, decimal amount);

    }

}
