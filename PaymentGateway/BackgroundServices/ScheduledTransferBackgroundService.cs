using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentGate.Domain.Entites;
using PaymentGate.Domain.Enums;
using PaymentGateway.Persistence;

namespace PaymentGateway.BackgroundServices
{
    public class ScheduledTransferBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScheduledTransferBackgroundService> _logger;

        public ScheduledTransferBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ScheduledTransferBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ScheduledTransferBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDueTransfersAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in ScheduledTransferBackgroundService.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("ScheduledTransferBackgroundService stopped.");
        }

        private async Task ProcessDueTransfersAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PaymentGatewayDbCOntext>();

            var dueTransfers = await db.ScheduledTransfers
                .Where(t =>
                    t.NextRunAt <= DateTime.UtcNow &&
                    t.TransferStatus == TransferStatus.Pending)
                .ToListAsync(stoppingToken);

            if (!dueTransfers.Any())
            {
                _logger.LogInformation("No scheduled transfers due at {time}.", DateTimeOffset.UtcNow);
                return;
            }

            _logger.LogInformation("Found {count} scheduled transfer(s) to process.", dueTransfers.Count);

            // ✅ Actually execute each due transfer
            foreach (var transfer in dueTransfers)
                await ExecuteTransferAsync(transfer, db, stoppingToken);
        }

        private async Task ExecuteTransferAsync(
            ScheduledTransfer transfer,
            PaymentGatewayDbCOntext db,
            CancellationToken stoppingToken)
        {
            using var tr = await db.Database.BeginTransactionAsync(stoppingToken);

            try
            {
                var source = await db.Wallets
                    .FirstOrDefaultAsync(w => w.WalletId == transfer.FromWallet, stoppingToken);

                var destination = await db.Wallets
                    .FirstOrDefaultAsync(w => w.WalletId == transfer.ToWallet, stoppingToken);

                if (source == null || destination == null)
                {
                    transfer.MarkFailed("One or both wallets no longer exist.");
                    await db.SaveChangesAsync(stoppingToken);
                    await tr.CommitAsync(stoppingToken);
                    _logger.LogWarning("Transfer {id} failed — wallet not found.",
                        transfer.Id);
                    return;
                }

                if (source.Balance < transfer.TotalAmount)
                {
                    if (transfer.IsRecurring)
                    {
                        transfer.AdvanceNextRun();
                        _logger.LogWarning(
                            "Transfer {id} skipped — insufficient balance. Next run at {next}.",
                            transfer.Id,
                            transfer.NextRunAt);
                    }
                    else
                    {
                        transfer.MarkFailed("Insufficient balance at execution time.");
                        _logger.LogWarning("Transfer {id} failed — insufficient balance.",
                            transfer.Id);
                    }

                    await db.SaveChangesAsync(stoppingToken);
                    await tr.CommitAsync(stoppingToken);
                    return;
                }

                // Debit source
                source.Debit(transfer.TotalAmount);

                var debitTx = new Transaction(
                    walletId: source.WalletId,
                    transferId: transfer.Id,
                    amount: transfer.TotalAmount,
                    currency: source.Currency,
                    type: TransactionType.Debit,
                    reference: Guid.NewGuid().ToString());

                // Credit destination
                destination.Credit(transfer.Amount);

                var creditTx = new Transaction(
                    walletId: destination.WalletId,
                    transferId: transfer.Id,
                    amount: transfer.Amount,
                    currency: destination.Currency,
                    type: TransactionType.Credit,
                    reference: Guid.NewGuid().ToString());

                debitTx.MarkAsCompleted();
                creditTx.MarkAsCompleted();

                db.Transactions.AddRange(debitTx, creditTx);

                transfer.MarkSuccess(debitTx.Reference, creditTx.Reference);

                if (transfer.IsRecurring)
                {
                    transfer.AdvanceNextRun();
                    _logger.LogInformation(
                        "Recurring transfer {id} executed. Next run at {next}.",
                        transfer.Id,
                        transfer.NextRunAt);
                }
                else
                {
                    _logger.LogInformation(
                        "One-time transfer {id} executed successfully.",
                        transfer.Id);  
                }

                await db.SaveChangesAsync(stoppingToken);
                await tr.CommitAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                await tr.RollbackAsync(stoppingToken);
                _logger.LogError(ex, "Error executing scheduled transfer {id}.",
                    transfer.Id); 

                transfer.MarkFailed($"System error: {ex.Message}");
                await db.SaveChangesAsync(stoppingToken);
            }
        }
    }
}