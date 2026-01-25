using PaymentGate.Domain.Enums;

namespace PaymentGate.Application.DTO
{
    public class FraudEvaluationResult
    {
        public decimal RiskScore { get; }
        public FraudDecision Decision { get; }
        public string Reason { get; }

        private FraudEvaluationResult(
            decimal riskScore,
            FraudDecision decision,
            string reason)
        {
            RiskScore = riskScore;
            Decision = decision;
            Reason = reason;
        }

        public static FraudEvaluationResult Approved(
            decimal riskScore,
            string reason) =>
            new(riskScore, FraudDecision.Approved, reason);

        public static FraudEvaluationResult Review(
            decimal riskScore,
            string reason) =>
            new(riskScore, FraudDecision.Review, reason);

        public static FraudEvaluationResult Rejected(
            decimal riskScore,
            string reason) =>
            new(riskScore, FraudDecision.Rejected, reason);
    }
}
