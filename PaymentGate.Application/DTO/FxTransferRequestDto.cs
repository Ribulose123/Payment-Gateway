using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO
{
    public class FxTransferRequestDto
    {
        public Guid FromWalletId { get; set; }
        public Guid ToWalletId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;

        public Guid IdempotencyKey { get; set; }
        public Guid InitiatorId { get; set; }
        public string? Description { get; set; }
        public string RequestHash { get; set; } = string.Empty;

        public void ComputeHash()
        {
            var raw = $"{InitiatorId}|{FromWalletId}|{ToWalletId}|{Amount}|{Currency}|{IdempotencyKey}";
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            RequestHash = Convert.ToHexString(bytes);
        }
    }
}
