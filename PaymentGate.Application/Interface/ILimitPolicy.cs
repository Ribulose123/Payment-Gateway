

using PaymentGate.Domain.Entites;

namespace PaymentGate.Application.Interface
{
    public interface ILimitPolicy
    {
        void Validate(User user, decimal amount);
        void Consume (User user, decimal amount);
    }
}
