using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentGateway.Persistence;
using Microsoft.EntityFrameworkCore;

namespace PaymentGateway.BackgroundServices
{
    public class LimitResetBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LimitResetBackgroundService> _logger;

        public LimitResetBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<LimitResetBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LimitResetBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PaymentGatewayDbCOntext>();

                    var userToReset = await db.Users.Where(x => x.LastLimitResetUtc.Date < DateTime.UtcNow.Date).ToListAsync(stoppingToken);


                    if (userToReset.Any())
                    {
                        foreach(var user in userToReset)
                        {
                            user.ResetDailyLimit();
                        }
                        await db.SaveChangesAsync(stoppingToken);


                        _logger.LogInformation(
                            "Reset daily limits for {count} users at {time}",
                            userToReset.Count,
                            DateTimeOffset.UtcNow);
                    }
                    else
                    {
                        _logger.LogInformation("No users needed limit reset at {time}",
                            DateTimeOffset.UtcNow);
                    }

                    var now =DateTime.UtcNow;
                    var nextMidNight = now.Date.AddDays(1);
                    var delay = nextMidNight - now;
                    // TODO: your logic here e.g. reset daily limits
                    _logger.LogInformation("Running limit reset at: {time}", DateTimeOffset.UtcNow);


                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in LimitResetBackgroundService.");

                    
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("LimitResetBackgroundService stopped.");
        }
    }
}