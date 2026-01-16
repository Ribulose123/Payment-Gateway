

using Microsoft.EntityFrameworkCore;

namespace PaymentGateway.Persistence
{
    public class PaymentGatewayDbCOntext:DbContext
    {
        public PaymentGatewayDbCOntext(DbContextOptions<PaymentGatewayDbCOntext> options):base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
