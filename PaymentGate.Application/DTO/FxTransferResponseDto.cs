using PaymentGate.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO
{
    public class FxTransferResponseDto
    {
        public Guid FxTransferId { get; set; }
        public Guid FromWalletId { get; set; }
        public Guid ToWalletId { get; set; }
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }

        public decimal Rate { get; set; }
        public decimal Fee { get; set; }
        public string? FailureReason { get; set; }
        public string Status { get; set; } = string.Empty;

        // Factory for Pending Review

        public static FxTransferResponseDto PendingReview(Guid transferId)
        {
            return new FxTransferResponseDto
            {
                FxTransferId = transferId,
                Status = FxEchangeStatus.Pending.ToString(),
                FailureReason = "Transfer pending fraud review",
            };
        }
    }
}
