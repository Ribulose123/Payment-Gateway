using PaymentGate.Domain.Enums;

namespace PaymentGate.Domain.Entites
{
    public class ScheduledTransfer
    {
        public Guid Id { get; private set; }
        public Guid InitiatorId { get; private set; }
        public Guid FromWallet { get; private set; }
        public Guid ToWallet { get; private set; }
        public decimal Amount { get; private set; }
        public decimal Fee { get; private set; }
        public decimal TotalAmount => Amount + Fee;
        public string Currency { get; private set; } = string.Empty;
        public TransferStatus TransferStatus { get; private set; }
        public DateTime ScheduleAt { get; private set; }
        public bool IsRecurring { get; private set; }
        public RecurrenceInterval RecurrenceInterval { get; private set; }
        public DateTime NextRunAt { get; private set; }
        public DateTime LastRunAt { get; private set; }
        public string? FailureReason { get; private set; }
        public string? DebitTransactionReference { get; private set; }
        public string? CreditTransactionReference { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private ScheduledTransfer() { }

        public ScheduledTransfer(
            Guid initiatorId,
            Guid fromWallet,
            Guid toWallet,
            decimal amount,
            string currency,
            decimal fee,
            DateTime scheduleAt,
            bool isRecurring,
            RecurrenceInterval recurrenceInterval)
        {
            if (fromWallet == toWallet)
                throw new ArgumentException("Source and destination wallets cannot be the same.");
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");
            if (fee < 0)
                throw new ArgumentException("Fee cannot be negative.");
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency is required.");
            if (scheduleAt <= DateTime.UtcNow)
                throw new ArgumentException("Scheduled time must be in the future.");

            Id = Guid.NewGuid();
            InitiatorId = initiatorId;
            FromWallet = fromWallet;
            ToWallet = toWallet;
            Amount = amount;
            Currency = currency.ToUpper();
            Fee = fee;
            ScheduleAt = scheduleAt;
            IsRecurring = isRecurring;
            RecurrenceInterval = isRecurring ? recurrenceInterval : RecurrenceInterval.None;
            NextRunAt = scheduleAt;       // ✅ first run = scheduled time
            TransferStatus = TransferStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public void MarkSuccess(string debitRef, string creditRef)
        {
            EnsurePending();
            DebitTransactionReference = debitRef
                ?? throw new ArgumentNullException(nameof(debitRef));
            CreditTransactionReference = creditRef
                ?? throw new ArgumentNullException(nameof(creditRef));
            LastRunAt = DateTime.UtcNow; 
            TransferStatus = TransferStatus.Success;
        }

        public void MarkFailed(string? reason = null)
        {
            EnsurePending();
            if (!string.IsNullOrWhiteSpace(reason))
                FailureReason = reason;
            LastRunAt = DateTime.UtcNow;
            TransferStatus = TransferStatus.Failed;
        }

       
        public void AdvanceNextRun()
        {
            if (!IsRecurring)
                throw new InvalidOperationException("Cannot advance a non-recurring transfer.");

            NextRunAt = RecurrenceInterval switch
            {
                RecurrenceInterval.Minutes => NextRunAt.AddMinutes(2),
                RecurrenceInterval.Daily => NextRunAt.AddDays(1),
                RecurrenceInterval.Weekly => NextRunAt.AddDays(7),
                RecurrenceInterval.Monthly => NextRunAt.AddMonths(1),
                _ => throw new InvalidOperationException("Unknown recurrence interval.")
            };

            TransferStatus = TransferStatus.Pending; 
        }

        
        public void Cancel()
        {
            if (TransferStatus == TransferStatus.Cancelled)
                throw new InvalidOperationException("Transfer is already cancelled.");
            if (TransferStatus == TransferStatus.Success && !IsRecurring)
                throw new InvalidOperationException("Completed transfers cannot be cancelled.");
            TransferStatus = TransferStatus.Cancelled;
        }

        public void MarkPendingReview(string reason)
        {
            EnsurePending();
            FailureReason = reason;
        }

        private void EnsurePending()
        {
            if (TransferStatus != TransferStatus.Pending)
                throw new InvalidOperationException("Only pending transfers can change status.");
        }
    }
}