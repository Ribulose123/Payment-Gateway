using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentGate.Application.DTO;
using PaymentGate.Application.Interface;

namespace PaymentGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreateWalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public CreateWalletController( IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateWallet(CreateWalletRequest request)
        {
            var wallet = await _walletService
                .CreateWalletAsync(request.UserId, request.Currency);

            return Ok(wallet);
        }
    }
}
