using PaymentGate.Domain.Enums;

namespace PaymentGate.Domain.Entites
{
    public class Reversal
    {
        public Guid ReversalId { get; private set; }

        public string TransactionReference { get; private set; } = string.Empty;

        public string Reason { get; private set; } = string.Empty;

        public ReversalStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }

        private Reversal() { }

        public Reversal(string transactionReference, string reason)
        {
            if (string.IsNullOrWhiteSpace(transactionReference))
                throw new ArgumentException("Transaction reference is required.");

            ReversalId = Guid.NewGuid();
            TransactionReference = transactionReference;
            Reason = reason;

            Status = ReversalStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public void MarkCompleted()
        {
            EnsurePending();
            Status = ReversalStatus.Completed;
        }

        public void MarkFailed(string reason)
        {
            EnsurePending();
            Reason = reason;
            Status = ReversalStatus.Failed;
        }

        public void Reject(string reason)
        {
            EnsurePending();
            Reason = reason;
            Status = ReversalStatus.Rejected;
        }

        private void EnsurePending()
        {
            if (Status != ReversalStatus.Pending)
                throw new InvalidOperationException("Only pending reversals can be updated.");
        }
    }
}
