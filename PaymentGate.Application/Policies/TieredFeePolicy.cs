

using PaymentGate.Application.Interface;
using PaymentGate.Domain.ValueObjects;

namespace PaymentGate.Application.Policies
{
    public class TieredFeePolicy:IFeePolicy
    {
        public FeeResult Calculate(decimal amount, string currency)
        {
            decimal fee;

            if(currency == "NGN")
            {
                if(amount < 10_00m)
                {
                    fee = 50m;
                } else if( amount <= 100_00m)
                {
                    fee = 100m;
                }
                else
                {
                    fee = 250m;
                }
            }
            else
            {
                if (amount <= 100m)
                    fee = 1m;
                else
                    fee = 3m;
            }

            return new FeeResult(amount, fee);
        }
    }
}
