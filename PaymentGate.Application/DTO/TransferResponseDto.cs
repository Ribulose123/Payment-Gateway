using PaymentGate.Domain.Enums;

namespace PaymentGate.Domain.DTO
{
    public class TransferResponseDto
    {
        public Guid TransferId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public Guid? DebitTransactionId { get; set; }
        public Guid? CreditTransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Factory for Pending Review
        public static TransferResponseDto PendingReview(Guid transferId)
        {
            return new TransferResponseDto
            {
                TransferId = transferId,
                Status = TransferStatus.Pending.ToString(),
                FailureReason = "Transfer pending fraud review",
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
