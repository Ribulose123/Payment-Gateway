
using PaymentGate.Domain.Enums;
using System.Globalization;

namespace PaymentGate.Domain.Entites
{
    public class Idempotency
    {
        public Guid IdempotencyId { get; private set; }
        public Guid Key { get; private set; }
        public Guid ClientId { get; private set; }

        public string RequsetHash { get; private set; } = string.Empty;
        public IdempotencyOperationType OperationType { get; private set; }
        public Guid? OperationRefernceId { get; private set; }
        public IdempotencyStatus Status { get; private set; }

        public string? ResponseSnapshot { get; private set; }

        public DateTime FirstSeenAt { get; private set; }
        public DateTime LastSeenAt { get; private set; }
        public DateTime ExpirationAt { get; private set; }

        private Idempotency() { }

        public Idempotency(Guid clientId, Guid key, string requestHash, IdempotencyOperationType operationType, TimeSpan ttl)
        {
            IdempotencyId = Guid.NewGuid();
            ClientId = clientId;
            Key = key;
            RequsetHash = requestHash;
            OperationType = operationType;
            Status = IdempotencyStatus.Processing;
            FirstSeenAt = DateTime.UtcNow;
            LastSeenAt = DateTime.UtcNow;
            ExpirationAt = FirstSeenAt.Add(ttl);
        }

        public void AttachOperationReference(Guid operationReferenceId)
        {
            OperationRefernceId = operationReferenceId;
        }

        public void MarkAsCompleted(string responseSnapshot)
        {
            Status = IdempotencyStatus.Completed;
            ResponseSnapshot = responseSnapshot;
            LastSeenAt = DateTime.UtcNow;
        }

        public void MarkAsFailed(string responseSnapshot)
        {
            Status = IdempotencyStatus.Failed;
            ResponseSnapshot = responseSnapshot;
            LastSeenAt = DateTime.UtcNow;
        }

        public void Touch()
        {
            LastSeenAt = DateTime.UtcNow;
        }

        public void ValidateRequestHash(string hash)
        {
            if (RequsetHash != hash)
                throw new InvalidOperationException(
                    "Idempotency key reused with different request payload.");
        }
    }
}
