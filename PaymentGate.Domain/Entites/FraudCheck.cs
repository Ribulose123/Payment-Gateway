using PaymentGate.Domain.Enums;

namespace PaymentGate.Domain.Entities
{
    public class FraudCheck
    {
        public Guid FraudCheckId { get; private set; }

        public Guid OperationId { get; private set; }
        public FraudOperationType OperationType { get; private set; }

        public decimal RiskScore { get; private set; }
        public FraudDecision Decision { get; private set; }

        public string Reason { get; private set; } = string.Empty;

        public string EvaluatedBy { get; private set; } = string.Empty;

        public DateTime EvaluatedAt { get; private set; }

        private FraudCheck() { }

        public FraudCheck(
            Guid operationId,
            FraudOperationType operationType,
            decimal riskScore,
            FraudDecision decision,
            string reason,
            string evaluatedBy)
        {
            FraudCheckId = Guid.NewGuid();

            OperationId = operationId;
            OperationType = operationType;

            RiskScore = riskScore;
            Decision = decision;

            Reason = reason;
            EvaluatedBy = evaluatedBy;

            EvaluatedAt = DateTime.UtcNow;
        }
    }
}
