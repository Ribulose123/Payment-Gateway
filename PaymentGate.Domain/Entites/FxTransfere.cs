

using PaymentGate.Domain.Enums;

namespace PaymentGate.Domain.Entites
{
    public class FxTransfere
    {
        public Guid FxTransfereId { get; private set; }
        public Guid SourceId { get; private set; }
        public Guid DestinationId { get; private set; }
        public decimal FromAmount { get; private set; }
        public decimal ToAmount { get; private set; }

        public decimal Rate { get; private set; }
        public decimal Fee { get; private set; }
        public string FromCurrency { get; private set; } = string.Empty;
        public string ToCurrency { get; private set; } = string.Empty;
        public FxTransfereStatus Status { get; private set; } = FxTransfereStatus.Pending;
        public DateTime CreatedAt { get; private set; }

        private FxTransfere() { }

        public FxTransfere(
            Guid sourceId,
            Guid destinationId,
            decimal fromAmount,
            decimal toAmount,
            decimal rate,
            decimal fee,
            string fromCurrency,
            string toCurrency)
        {
            if (fromAmount <= 0)
                throw new Exception("From amount must be greater than zero");
            if (toAmount <= 0)
                throw new Exception("To amount must be greater than zero");
            if (string.IsNullOrWhiteSpace(fromCurrency))
                throw new Exception("From currency is required");
            if (string.IsNullOrWhiteSpace(toCurrency))
                throw new Exception("To currency is required");
            FxTransfereId = Guid.NewGuid();
            SourceId = sourceId;
            DestinationId = destinationId;
            FromAmount = fromAmount;
            ToAmount = toAmount;
            Rate = rate;
            Fee = fee;
            FromCurrency = fromCurrency;
            ToCurrency = toCurrency;
            CreatedAt = DateTime.UtcNow;
        }

        public void MarkAsCompleted()
        {
            if (Status != FxTransfereStatus.Pending)
                throw new Exception("Only pending FX transfers can be completed");
            Status = FxTransfereStatus.Success;
        }

        public void MarkAsFailed()
        {
            if (Status != FxTransfereStatus.Pending)
                throw new Exception("Only pending FX transfers can be failed");
            Status = FxTransfereStatus.Failed;


        }
    }
}
