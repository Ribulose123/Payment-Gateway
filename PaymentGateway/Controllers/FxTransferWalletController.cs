using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentGate.Application.Interface;

namespace PaymentGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FxTransferWalletController : ControllerBase
    {
        private readonly IFxTransfer _fxTransfer;

        public FxTransferWalletController(IFxTransfer fxTransfer)
        {
            _fxTransfer = fxTransfer;
        }

        [HttpPost]
        public async Task<IActionResult> FxRequest([FromBody] PaymentGate.Application.DTO.FxTransferRequestDto requestDto)
        {
            if(requestDto == null)
                return BadRequest("Request data is null.");

            try
            {
                var result = await _fxTransfer.FxTransFereAsync(requestDto);
                return Ok(result);
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
