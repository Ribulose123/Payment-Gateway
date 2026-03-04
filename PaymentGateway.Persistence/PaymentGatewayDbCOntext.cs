

using Microsoft.EntityFrameworkCore;
using PaymentGate.Domain.Entites;
using PaymentGate.Domain.Entities;

namespace PaymentGateway.Persistence
{
    public class PaymentGatewayDbCOntext:DbContext
    {
        public PaymentGatewayDbCOntext(DbContextOptions<PaymentGatewayDbCOntext> options):base(options)
        {
            
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<decimal>()
                .HavePrecision(18, 4);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Wallet>()
                .Property(i => i.Balance)
                .HasPrecision(18, 2);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Transfer> Transfers { get; set; }
        public DbSet<FraudCheck> FraudChecks { get; set; }
        public DbSet<Reversal> Reversals { get; set; }
        public DbSet<Idempotency> Idempotencies { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<FxExchange> FxExchanges { get; set; }
        public DbSet<FxTransfer> FxTransfers { get; set; }
    }
}
