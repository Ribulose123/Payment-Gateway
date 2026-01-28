using PaymentGate.Domain.Enums;
using System.Net.Mail;

namespace PaymentGate.Domain.Entites
{
    public class User
    {
        public Guid UserId { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public AccountStatus Status { get; private set; }
        public DateOnly CreatedAt { get; private set; }
        public decimal DailyTransferLimit { get; private set; }
        public decimal DailyLimitUsed { get; private set; }
        public DateTime LastLimitResetUtc { get; private set; }

        private const decimal DEFAULT_DAILY_LIMIT = 100_000m;

        private User() { }

        public User(string email)
        {
            UserId = Guid.NewGuid();
            SetEmail(email);
            Status = AccountStatus.Active;
            DailyTransferLimit = DEFAULT_DAILY_LIMIT;
            DailyLimitUsed = 0;
            LastLimitResetUtc = DateTime.UtcNow;
            CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        public void Suspend()
        {
            Status = AccountStatus.Suspended;
        }

        public void Activate()
        {
            Status = AccountStatus.Active;
        }

        public void ResetDailyLimit()
        {
            if(LastLimitResetUtc.Date !=  DateTime.UtcNow)
            {
                DailyLimitUsed = 0;
                LastLimitResetUtc = DateTime.UtcNow;
            }
        }

        public void ValidateDailyLimit (decimal amount)
        {
            if (DailyLimitUsed + amount > DailyTransferLimit)
                throw new Exception("Daily transfer limit exceeded");
        }

        public void ConsumeDailyLimit(decimal amount)
        {
            DailyLimitUsed += amount;
        }

        public void UpdateDailyLimit(decimal newLimit)
        {
            if (newLimit <= 0)
                throw new Exception("Limit must be positive");

            DailyTransferLimit = newLimit;
        }



        private void SetEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty.");

            try
            {
                var addr = new MailAddress(email);
                Email = addr.Address;
            }
            catch
            {
                throw new ArgumentException("Invalid email format.");
            }
        }
    }
}
