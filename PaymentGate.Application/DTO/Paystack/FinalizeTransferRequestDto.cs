using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO.Paystack
{
    public class FinalizeTransferRequestDto
    {
        public string TransferCode { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }
}
