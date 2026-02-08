
using PaymentGate.Application.DTO;
using PaymentGate.Domain.Entites;
using PaymentGate.Domain.Entities;

namespace PaymentGate.Application.Interface
{
    public interface IFxFraudPolicy
    {
        FraudEvaluationResult Evaluate(FxExchange fxExchange, Wallet source, Wallet destination);
    }
}
