using PaymentGate.Domain.Enums;

namespace PaymentGate.Domain.Entites
{
    public class Wallet
    {
        public Guid WalletId { get; private set; }
        public Guid UserId { get; private set; }
        public decimal Balance { get; private set; }
        public string Currency {  get; private set; } = string.Empty;
        public WalletStatus Status {  get; private set; }

        private Wallet() { }

        public Wallet(Guid userId, string currency)
        {
            UserId = userId;
            WalletId = Guid.NewGuid();
            Balance = 0;
            Currency = currency;
            Status = WalletStatus.Active;
        }

        public void Credit(decimal amount)
        {
            EnsureNotFrozen();
            if (amount <= 0)
                throw new Exception("Ivalid Amount");

            Balance += amount;
        }

        public void Debit (decimal amount)
        {
            EnsureNotFrozen();
            if (amount <= 0) throw new Exception("Invalid Amount");
            if (amount > Balance) throw new Exception("Insufficent fund");

            Balance -= amount;
        }

        public void Freeze()
        {
            Status = WalletStatus.Frozen;
        }

        public void Activate()
        {
            Status = WalletStatus.Active;
        }

        private void EnsureNotFrozen()
        {
           if(Status == WalletStatus.Frozen)
                throw new Exception("Wallet is frozen");
        }
    }
}
