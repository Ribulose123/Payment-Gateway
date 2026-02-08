using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO
{
    public class ExchangeServiceDto
    {
        public Guid UserId { get; set; }
        public Guid FromWalletId { get; set; }
        public Guid ToWalletId { get; set; }
        public decimal Amount { get; set; }
     
        public Guid IdempotencyKey { get; set; }
        public Guid InitiatorId { get; set; }
        public string? Description { get; set; }
        public string RequsetHash { get; private set; } = string.Empty;

    }
}
