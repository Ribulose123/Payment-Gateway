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
            if (transfer.Amount > 1_000_000)
                return FraudEvaluationResult.Rejected(
                    riskScore: 95,
                    reason: "Transfer amount exceeds fraud threshold");

            if (transfer.Amount > 100_000)
                return FraudEvaluationResult.Review(
                    riskScore: 70,
                    reason: "High value transfer requires manual review");

            return FraudEvaluationResult.Approved(
                riskScore: 10,
                reason: "Low risk transfer");
        }

        // ✅ FIX: Implement instead of throwing
        public FraudEvaluationResult Evaluate(
            FxTransfer transfer,
            Wallet source,
            Wallet destination)
        {
            // ✅ Use FromAmount since FxTransfer has no single Amount property
            if (transfer.FromAmount > 1_000_000)
                return FraudEvaluationResult.Rejected(
                    riskScore: 95,
                    reason: "FX transfer amount exceeds fraud threshold");

            if (transfer.FromAmount > 100_000)
                return FraudEvaluationResult.Review(
                    riskScore: 70,
                    reason: "High value FX transfer requires manual review");

            return FraudEvaluationResult.Approved(
                riskScore: 10,
                reason: "Low risk FX transfer");
        }
    }
}