using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace PaymentGateway.Persistence
{
    public class PaymentGatewayDbContextFactory : IDesignTimeDbContextFactory<PaymentGatewayDbCOntext>
    {
        public PaymentGatewayDbCOntext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<PaymentGatewayDbCOntext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connectionString);

            return new PaymentGatewayDbCOntext(optionsBuilder.Options);
        }
    }
}