using PaymentGate.Application.Interface;
using PaymentGate.Domain.ValueObjects;

public class FlatFeePolicy : IFeePolicy
{
    public FeeResult Calculate(decimal amount, string currency)
    {
        decimal fee = currency == "NGN" ? 100m : 1m;
        return new FeeResult(amount, fee);
    }
}
