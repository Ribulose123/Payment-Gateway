
using PaymentGate.Domain.ValueObjects;


namespace PaymentGate.Application.Interface
{
    public interface IFeePolicy
    {
        FeeResult Calculate(decimal amount, string currency);
    }
}
