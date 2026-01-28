using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO
{
    public class CreateWalletRequest
    {
        public Guid UserId { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
