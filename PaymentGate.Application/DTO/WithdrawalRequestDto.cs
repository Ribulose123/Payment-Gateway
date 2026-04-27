using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO
{
    public class WithdrawalRequestDto
    {
        public Guid InitiatorId { get; set; }
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; }
        public string RecipientCode { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequsetHash { get; private set; } = string.Empty;
        public Guid IdempotencyKey { get; set; }
    }
}
