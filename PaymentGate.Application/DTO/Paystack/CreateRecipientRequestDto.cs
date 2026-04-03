using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO.Paystack
{
    public class CreateRecipientRequestDto
    {
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
    }
}
