using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO.Paystack
{
    public class TransferRecipientResponseDto
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RecipientCode { get; set; } = string.Empty;  
    }
}
