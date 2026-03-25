using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO
{
    public class ScheduledTransferResponseDto
    {
        public Guid ScheduleTransferId { get; set; }
        public Guid FromWalletId { get; set; }
        public Guid ToWalletId { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ScheduleAt { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrenceInterval { get; set; }   
        public DateTime NextRunAt { get; set; }
        public DateTime? LastRunAt { get; set; }        
        public string? FailureReason { get; set; }  
        public string? DebitTransactionReference { get; set; }
        public string? CreditTransactionReference { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
