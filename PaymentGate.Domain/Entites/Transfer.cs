
using PaymentGate.Domain.Enums;

namespace PaymentGate.Domain.Entites
{
    public class Transfer
    {
        public Guid TransferId { get; private set; }
        public Guid SourceWalletId { get; private set; }   
        public Guid DestinationWalletId { get; private set; }
        public decimal Amount { get; private set; }
        public string Currency { get; private set; } = string.Empty;
        public TransferStatus Status { get; private set; }
        public string? Description { get; private set; }
        public string? DebitTransactionReference { get; private set; }  
        public string? CreditTransactionReference { get; private set; }
        public DateOnly CreatedAt { get; private set; }

        private Transfer() { }


        public Transfer(
            Guid sourceWalletId,
            Guid destinationWalletId,
            decimal amount,
            string currency,
            string? description = null)
        {
            if (sourceWalletId == destinationWalletId)
                throw new ArgumentException("Source and destination wallets cannot be the same.");

            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency is required.");

            TransferId = Guid.NewGuid();
            SourceWalletId = sourceWalletId;
            DestinationWalletId = destinationWalletId;
            Amount = amount;
            Currency = currency.ToUpper();
            Description = description;

            Status = TransferStatus.Pending;
            CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
        }


        public void MarkSuccess(string debitRef, string creditRef)
        {
            EnsurePending();

            DebitTransactionReference = debitRef
                ?? throw new ArgumentNullException(nameof(debitRef));

            CreditTransactionReference = creditRef
                ?? throw new ArgumentNullException(nameof(creditRef));

            Status = TransferStatus.Success;
        }

        public void MarkFailed(string? reason = null)
        {
            EnsurePending();

            if (!string.IsNullOrWhiteSpace(reason))
                Description = reason;

            Status = TransferStatus.Failed;
        }

        private void EnsurePending()
        {
            if (Status != TransferStatus.Pending)
                throw new InvalidOperationException("Only pending transfers can change status.");
        }
    }
}
