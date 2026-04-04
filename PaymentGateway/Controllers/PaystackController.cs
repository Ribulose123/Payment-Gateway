using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentGate.Application.DTO.Paystack;
using PaymentGate.Application.Interface;

namespace PaymentGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaystackController : ControllerBase
    {
        private readonly IPaystackService _paystackService;

        public PaystackController(IPaystackService paystackService)
        {
            _paystackService = paystackService;
        }

        [HttpPost("initialize")]

        public async Task<IActionResult> InitializePayment([FromBody] InitializePaymentRequestDto request)
        {
            var response = await _paystackService.InitializePaymentAsync(request);
            return Ok(response);
        }

        [HttpGet("verify/{reference}")]
        public async Task<IActionResult> VerifyPayment(string reference)
        {
            var respones = await _paystackService.VerifyPaymentAsync(reference);
            return Ok(new {verified = respones });
        }

        [HttpPost("recipient")]
        public async Task<IActionResult> CreateRecipient(
           [FromBody] CreateRecipientRequestDto request)
        {
            var response = await _paystackService.CreateTransferRecipientAsync(request);
            return Ok(response);
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> InitiateTransfer(
            [FromBody] InitiateTransferRequestDto request)
        {
            var response = await _paystackService.InitiateTransferAsync(request);
            return Ok(response);
        }

        [HttpPost("virtual-account")]
        public async Task<IActionResult> CreateVirtualAccount(
           [FromBody] CreateVirtualAccountRequestDto request)
        {
            var response = await _paystackService.CreateVirtualAccountAsync(request);
            return Ok(response);
        }
    }
}
