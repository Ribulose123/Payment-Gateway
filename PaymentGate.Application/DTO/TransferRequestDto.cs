using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Domain.DTO
{
    public class TransferRequestDto
    {
        public Guid SourceWalletId { get; set; }
        public Guid DestinationWalletId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
        public Guid InitiatorId { get; set; }
        public string? Description { get; set; }
    }
}
