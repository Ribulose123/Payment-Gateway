using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO.Paystack
{
    public class FinalizeResponseDto
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TransferCode { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }
}
