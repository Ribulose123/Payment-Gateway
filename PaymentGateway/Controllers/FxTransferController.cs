using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentGate.Application.Interface;

namespace PaymentGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FxTransferController : ControllerBase
    {
        private readonly IWalletExchangeService _walletExchangeService;

        public FxTransferController(IWalletExchangeService walletExchangeService)
        {
            _walletExchangeService = walletExchangeService;
        }

        [HttpPost]

        public async Task<IActionResult> FxTransferRequest([FromBody] PaymentGate.Application.DTO.ExchangeServiceDto request)
        {
            if (request == null)
                return BadRequest("Requet body is requried");
            try
            {
                var response = await _walletExchangeService.ExchangeAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message
                });
            }
        }
    }
}
