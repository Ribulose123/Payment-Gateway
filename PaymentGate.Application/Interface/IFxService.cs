using PaymentGate.Application.Policies;

namespace PaymentGate.Application.Interface
{
    public interface IFxService
    {
        Task<FxQuote> QuoteAsync(string from, string to, decimal amount);
    }

}
