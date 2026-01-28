

namespace PaymentGate.Application.Policies
{
    public class FxQuote
    {
        public decimal Rate { get; }
        public decimal FromAmount { get; }
        public decimal ToAmount { get; }

        public FxQuote(decimal rate, decimal from, decimal to)
        {
            Rate = rate;
            FromAmount = from;
            ToAmount = to;
        }
    }
}
