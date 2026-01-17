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

        private User() { }

        public User(string email)
        {
            UserId = Guid.NewGuid();
            SetEmail(email);
            Status = AccountStatus.Active;
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
