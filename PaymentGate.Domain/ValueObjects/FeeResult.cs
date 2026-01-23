

namespace PaymentGate.Domain.ValueObjects
{
    public class FeeResult
    {
        public decimal Fee { get; }
        public decimal TotalDebit { get; }

        public FeeResult(decimal amount, decimal fee)
        {
            Fee = fee;
            TotalDebit = amount + fee;
        }
    }
}
