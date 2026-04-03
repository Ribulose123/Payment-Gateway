using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO.Paystack
{
    public class InitializePaymentResponseDto
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AuthorizationUrl { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string AccessCode { get; set; } = string.Empty;
    }
}
