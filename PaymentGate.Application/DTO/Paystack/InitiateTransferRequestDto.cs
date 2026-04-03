using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO.Paystack
{
    public class InitiateTransferRequestDto
    {
        public decimal Amount { get; set; }
        public string RecipientCode { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public Guid WalletId { get; set; }
    }
}
