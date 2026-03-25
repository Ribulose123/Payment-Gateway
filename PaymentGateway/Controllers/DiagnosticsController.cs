using Microsoft.AspNetCore.Mvc;
using PaymentGate.Domain.Enums;
using PaymentGateway.Persistence;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly PaymentGatewayDbCOntext _context;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController( PaymentGatewayDbCOntext context, ILogger<DiagnosticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("check-transfers")]
    public async Task<IActionResult> CheckTransfers()
    {
        var now = DateTime.UtcNow;
        _logger.LogInformation("Checking transfers at UTC time: {Now}", now);

        var allPending = await _context.ScheduledTransfers
            .Where(t => t.TransferStatus == TransferStatus.Pending)
            .ToListAsync();

        var dueTransfers = await _context.ScheduledTransfers
            .Where(t => t.NextRunAt <= now && t.TransferStatus == TransferStatus.Pending)
            .ToListAsync();

        return Ok(new
        {
            CurrentUtcTime = now,
            CurrentLocalTime = DateTime.Now,
            TotalPending = allPending.Count,
            DueTransfers = dueTransfers.Select(t => new
            {
                t.Id,
                t.ScheduleAt,
                t.NextRunAt,
                t.TransferStatus,
                t.IsRecurring,
                TimeUntilDue = (t.NextRunAt - now).TotalMinutes,
                IsDue = t.NextRunAt <= now
            }),
            AllPendingTransfers = allPending.Select(t => new
            {
                t.Id,
                t.ScheduleAt,
                t.NextRunAt,
                t.TransferStatus,
                TimeUntilDue = (t.NextRunAt - now).TotalMinutes
            })
        });
    }

    [HttpGet("force-process")]
    public async Task<IActionResult> ForceProcessTransfers()
    {
        try
        {
            // Manually trigger processing of all pending transfers
            var pendingTransfers = await _context.ScheduledTransfers
                .Where(t => t.TransferStatus == TransferStatus.Pending)
                .ToListAsync();

            foreach (var transfer in pendingTransfers)
            {
                // Use a method to update NextRunAt since the setter is private
                typeof(PaymentGate.Domain.Entites.ScheduledTransfer)
                    .GetProperty("NextRunAt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                    ?.SetValue(transfer, DateTime.UtcNow);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Updated {pendingTransfers.Count} transfers to be processed immediately",
                Transfers = pendingTransfers.Select(t => t.Id)
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}