using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Domain.Entites
{
    public class AuditLog
    {
        public Guid Id { get; private set; }

        public string EntityName { get; private set; } = string.Empty;
        public Guid EntityId { get; private set; }

        public string Action { get; private set; } = string.Empty;
        public string PerformedBy { get; private set; } = string.Empty;

        public string? OldValue { get; private set; }
        public string? NewValue { get; private set; }

        public DateTime CreatedAt { get; private set; }

        private AuditLog() { }

        public AuditLog(
            string entityName,
            Guid entityId,
            string action,
            string performedBy,
            string? oldValue,
            string? newValue)
        {
            Id = Guid.NewGuid();
            EntityName = entityName;
            EntityId = entityId;
            Action = action;
            PerformedBy = performedBy;
            OldValue = oldValue;
            NewValue = newValue;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
