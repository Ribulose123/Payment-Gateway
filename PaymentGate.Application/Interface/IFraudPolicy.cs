using PaymentGate.Domain.Entites;
using PaymentGate.Application.DTO;


namespace PaymentGate.Application.Interface
{
    public interface IFraudPolicy
    {
        FraudEvaluationResult Evaluate(Transfer transfer, Wallet source, Wallet destination);
        object Evaluate(FxTransfer transfer, Wallet source, Wallet destination);
    }

}
