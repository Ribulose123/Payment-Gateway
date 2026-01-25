using PaymentGate.Application.DTO;
using PaymentGate.Application.Interface;
using PaymentGate.Domain.Entites;

namespace PaymentGate.Application.Services
{
    public class BasicFraudPolicy : IFraudPolicy
    {
        public FraudEvaluationResult Evaluate(
            Transfer transfer,
            Wallet source,
            Wallet destination)
        {
            // Hard rule: unusually large transfers
            if (transfer.Amount > 1_000_000)
            {
                return FraudEvaluationResult.Rejected(
                    riskScore: 95,
                    reason: "Transfer amount exceeds fraud threshold"
                );
            }

            // Medium risk: large but acceptable
            if (transfer.Amount > 100_000)
            {
                return FraudEvaluationResult.Review(
                    riskScore: 70,
                    reason: "High value transfer requires manual review"
                );
            }

            // Low risk
            return FraudEvaluationResult.Approved(
                riskScore: 10,
                reason: "Low risk transfer"
            );
        }
    }
}
