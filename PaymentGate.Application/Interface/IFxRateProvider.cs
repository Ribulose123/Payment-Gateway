

namespace PaymentGate.Application.Interface
{
    public interface IFxRateProvider
    {
        Task<decimal> GetRateAsync(string fromCurrency, string toCurrency);
    }

}
