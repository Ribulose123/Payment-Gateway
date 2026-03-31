using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO.Paystack
{
    public class InitializePaymentRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public decimal Amount { get; set; } 
        public string Reference { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public Guid WalletId { get; set; }
    }
}
