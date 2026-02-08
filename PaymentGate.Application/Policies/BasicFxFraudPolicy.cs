using PaymentGate.Application.DTO;
using PaymentGate.Application.Interface;
using PaymentGate.Domain.Entites;
using PaymentGate.Domain.Entities;

namespace PaymentGate.Application.Services
{
    public class BasicFxFraudPolicy : IFxFraudPolicy
    {
        private const decimal HardLimit = 5_000_000m;
        private const decimal ReviewLimit = 1_000_000m;

        public FraudEvaluationResult Evaluate(
            FxExchange fxExchange,
            Wallet source,
            Wallet destination)
        {
            // 🔒 Sanity check (defensive)
            if (source.UserId != destination.UserId)
            {
                return FraudEvaluationResult.Rejected(
                    riskScore: 100,
                    reason: "FX exchange across different users detected"
                );
            }

            // 🚨 Hard rule: extremely large FX conversion
            if (fxExchange.FromAmount >= HardLimit)
            {
                return FraudEvaluationResult.Rejected(
                    riskScore: 95,
                    reason: "FX amount exceeds maximum allowed threshold"
                );
            }

            // ⚠️ Medium risk: large FX conversion
            if (fxExchange.FromAmount >= ReviewLimit)
            {
                return FraudEvaluationResult.Review(
                    riskScore: 65,
                    reason: "High-value FX conversion requires review"
                );
            }

            // 🧠 Suspicious rate check (optional guard)
            if (fxExchange.Rate <= 0)
            {
                return FraudEvaluationResult.Rejected(
                    riskScore: 90,
                    reason: "Invalid FX rate detected"
                );
            }

            // ✅ Low risk
            return FraudEvaluationResult.Approved(
                riskScore: 10,
                reason: "FX exchange within normal parameters"
            );
        }
    }
}
