namespace PaymentGate.Domain.ValueObjects
{
    public class FxQuote
    {
        public decimal Rate { get; }
        public decimal ConvertedAmount { get; }
        public DateTime QuotedAt { get; }

        public FxQuote(decimal rate, decimal convertedAmount)
        {
            Rate = rate;
            ConvertedAmount = convertedAmount;
            QuotedAt = DateTime.UtcNow;
        }
    }
}
