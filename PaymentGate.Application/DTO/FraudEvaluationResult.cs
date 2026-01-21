using PaymentGate.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.DTO
{
    public class FraudEvaluationResult
    {
        public decimal RiskScore { get; set; }
        public FraudDecision Decision { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

}
