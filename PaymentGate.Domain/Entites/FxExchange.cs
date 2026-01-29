using PaymentGate.Domain.Enums;

namespace PaymentGate.Domain.Entities
{
    public class FxExchange
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }

        public Guid FromWalletId { get; private set; }
        public Guid ToWalletId { get; private set; }

        public decimal FromAmount { get; private set; }
        public decimal ToAmount { get; private set; }

        public decimal Rate { get; private set; }
        public decimal Fee { get; private set; }

        public string FromCurrency { get; private set; } = string.Empty;
        public string ToCurrency { get; private set; } = string.Empty;

        public FxEchangeStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private FxExchange() { }

        public FxExchange(
            Guid userId,
            Guid fromWalletId,
            Guid toWalletId,
            decimal fromAmount,
            decimal toAmount,
            decimal rate,
            decimal fee,
            string fromCurrency,
            string toCurrency)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            FromWalletId = fromWalletId;
            ToWalletId = toWalletId;
            FromAmount = fromAmount;
            ToAmount = toAmount;
            Rate = rate;
            Fee = fee;
            FromCurrency = fromCurrency;
            ToCurrency = toCurrency;
            Status = FxEchangeStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public void MarkCompleted() => Status = FxEchangeStatus.Success;
        public void MarkFailed() => Status = FxEchangeStatus.Failed;
    }
}
