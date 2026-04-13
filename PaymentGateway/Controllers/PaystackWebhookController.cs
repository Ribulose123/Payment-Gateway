using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentGate.Domain.Entites;
using PaymentGate.Domain.Enums;
using PaymentGateway.Persistence;
using Paystack;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PaymentGateway.Controllers
{
    [Route("api/paystack")]  
    [ApiController]
    public class PaystackWebhookController : ControllerBase
    {
        private readonly PaymentGatewayDbCOntext _context;
        private readonly PaystackOptions _options;
        private readonly ILogger<PaystackWebhookController> _logger;

        public PaystackWebhookController(
            PaymentGatewayDbCOntext context,
            IOptions<PaystackOptions> options,
            ILogger<PaystackWebhookController> logger)
        {
            _context = context;
            _options = options.Value;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebHook()
        {
            Request.EnableBuffering();

            using var reader = new StreamReader(
                Request.Body,
                Encoding.UTF8,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();

            
            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogWarning("Webhook received with empty body.");
                return Ok();
            }


            if (!IsValidSignature(body, Request.Headers["x-paystack-signature"]))
            {
                _logger.LogWarning("Invalid Paystack webhook signature.");
                return Unauthorized();
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var eventType = root.GetProperty("event").GetString();
            _logger.LogInformation("Paystack webhook received: {event}", eventType);

            if (eventType == "charge.success")
                await HandleChargeSuccess(root);

            return Ok();
        }


        [HttpGet("banks")]
        public async Task<IActionResult> GetBanks()
        {
            var httpContent = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();
            httpContent.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.SecretKey);

            var response = await httpContent.GetAsync("https://api.paystack.co/bank");
            var content = await response.Content.ReadAsStringAsync();
            return Ok(content);
        }
        private async Task HandleChargeSuccess(JsonElement root)
        {
            try
            {
                var data = root.GetProperty("data");

                var reference = data.GetProperty("reference").GetString()!;
                var amountInKobo = data.GetProperty("amount").GetInt64();
                var amountInNaira = amountInKobo / 100m;


                var metadata = data.GetProperty("metadata");
                var walletString = metadata.GetProperty("wallet_id").GetString()!;
                var walletId = Guid.Parse(walletString);

                _logger.LogInformation(
                    "Processing deposit — Reference: {ref}, Amount: {amount}, WalletId: {walletId}",
                    reference, amountInNaira, walletId);

                // Check duplicate
                var alreadyProcessed = await _context.Transactions
                    .AnyAsync(r => r.Reference == reference);

                if (alreadyProcessed)
                {
                    _logger.LogWarning("Duplicate webhook — reference {ref} already processed.", reference);
                    return;
                }

                // Load wallet
                var wallet = await _context.Wallets
                    .FirstOrDefaultAsync(w => w.WalletId == walletId);

                if (wallet == null)
                {
                    _logger.LogError("Wallet {walletId} not found for deposit.", walletId);
                    return;
                }

                // Credit wallet
                wallet.Credit(amountInNaira);

                // Create transaction record
                var transaction = new Transaction(
                    walletId: wallet.WalletId,
                    transferId: Guid.Empty,
                    amount: amountInNaira,
                    currency: wallet.Currency,
                    type: TransactionType.Credit,
                    reference: reference);

                transaction.MarkAsCompleted();

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Wallet {walletId} credited {amount} NGN. New balance: {balance}",
                    walletId, amountInNaira, wallet.Balance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing charge.success webhook.");
            }
        }

        private bool IsValidSignature(string body, string? signature)
        {
            if (string.IsNullOrEmpty(signature))
                return false;

            using var hmac = new HMACSHA512(
                Encoding.UTF8.GetBytes(_options.WebhookSecret));

            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            var computedSignature = Convert.ToHexString(hash).ToLower();

            return computedSignature == signature;
        }
    }
}