using PaymentGate.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Domain.Entites
{
    public class WithdrawalRequest
    {
        public Guid Id { get; private set; }
        public Guid WalletId { get; private set; }
        public decimal Amount { get; private set; }
        public string Reference { get; private set; } = string.Empty;
        public string RecipietCode { get; private set; } = string.Empty;
        public string TransferCode { get; private set; } = string.Empty;
        public WithdrawalStatus Status { get; private set; }
        public string? FailureReason { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private WithdrawalRequest() { }

        public WithdrawalRequest( Guid walletId, decimal amount, string reference, string recipientCode)
        {
            if(amount <= 0)
              throw new ArgumentException("Amount must be greater than zero.");

            if (string.IsNullOrEmpty(reference))
                throw new ArgumentException("Reference is requried");
            if (string.IsNullOrEmpty(recipientCode))
                throw new ArgumentException("Recipient code is requried");

            Id = Guid.NewGuid();
            WalletId = walletId;
            Amount = amount;
            Reference = reference;
            RecipietCode = recipientCode;
            Status = WithdrawalStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public void AttachTransferCode(string transferCode)
        {
            TransferCode = transferCode;
        }

        public void MarkSuccess()
        {
            Status = WithdrawalStatus.Success;
        }

        public void MarkFailed(string reason)
        {
            Status = WithdrawalStatus.Failed;
            FailureReason = reason;
        }


    }
}
