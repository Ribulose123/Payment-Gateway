using PaymentGate.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO
{
    public class ScheduledTransferRequestDto
    {
        public Guid InitiatorId { get; set; }
        public Guid FromWalletId { get; set; }
        public Guid ToWalletId { get; set; }
        public decimal Amount { get; set; }
        public Guid IdempotencyKey { get; set; }
        public string RequestHash { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public DateTime ScheduleAt { get; set; }
        public bool IsRecurring { get; set; }
        public RecurrenceInterval RecurrenceInterval { get; set; }
        public string? Description { get; set; }
    }
}
