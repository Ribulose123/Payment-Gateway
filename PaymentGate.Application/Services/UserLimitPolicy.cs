

using PaymentGate.Application.Interface;
using PaymentGate.Domain.Entites;

namespace PaymentGate.Application.Services
{
    public class UserLimitPolicy:ILimitPolicy
    {
        public void Validate(User user, decimal amount)
        {
            user.ResetDailyLimit();
            user.ValidateDailyLimit(amount);
        }

        public void Consume(User user, decimal amount)
        {
            user.ConsumeDailyLimit(amount);
        }
    }
}
