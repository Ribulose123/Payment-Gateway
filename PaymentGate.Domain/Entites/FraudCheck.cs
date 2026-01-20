

using PaymentGate.Domain.Enums;

namespace PaymentGate.Domain.Entites
{
    public class FraudCheck
    {
        public Guid FraudCheckId { get; private set; }
        public Guid OperationId { get; private set; }   
        public FraudOperationType OperationType { get; private set; }

        public FraudDecision Decision { get; private set; }
        public string Reason { get; private set; } = string.Empty;

        public decimal RiskScore { get; private set; }  // 0 - 100

        public DateTime EvaluatedAt { get; private set; }

        private FraudCheck() { }

        public FraudCheck(Guid operationId, FraudOperationType operationType, decimal riskScore, string reason)
        {
            FraudCheckId = Guid.NewGuid();
            OperationId = operationId;
            OperationType = operationType;
            RiskScore = riskScore;
            Reason = reason;
            EvaluatedAt = DateTime.UtcNow;

            Decision = CalculateDecision(riskScore);
        }

        private FraudDecision CalculateDecision(decimal score)
        {
            if (score < 30) return FraudDecision.Approved;
            if (score < 70) return FraudDecision.Review;
            return FraudDecision.Rejected;
        }
    }
}
