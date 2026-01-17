using PaymentGate.Domain.Enums;

namespace PaymentGate.Domain.Entites
{
    public class Transaction
    {
        public Guid TransactionId { get; private set; }
        public Guid WalletId { get; private set; }
        public Guid TransferId { get; private set; }
        public decimal Amount { get; private set; }
        public string Currency { get; private set; } = string.Empty;
        public TransactionType Type { get; private set; }
        public string Reference { get; private set; } = string.Empty;
        public TransactionStatus Status { get; private set; }
        public DateOnly CreatedDate { get; private set; }

        private Transaction() { }

        public Transaction(
            Guid walletId,
            Guid transferId,
            decimal amount,
            string currency,
            TransactionType type,
            string reference)
        {
            if (amount <= 0)
                throw new Exception("Invalid amount");

            if (string.IsNullOrWhiteSpace(reference))
                throw new Exception("Reference is required");

            TransactionId = Guid.NewGuid();
            WalletId = walletId;
            TransferId = transferId;
            Amount = amount;
            Currency = currency;
            Type = type;
            Reference = reference;
            Status = TransactionStatus.Pending;
            CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        public void MarkAsCompleted()
        {
            if (Status != TransactionStatus.Pending)
                throw new Exception("Only pending transactions can be completed");

            Status = TransactionStatus.Completed;
        }

        public void MarkAsFailed()
        {
            if (Status != TransactionStatus.Pending)
                throw new Exception("Only pending transactions can be failed");

            Status = TransactionStatus.Failed;
        }
    }
}
