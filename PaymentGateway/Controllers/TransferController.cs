using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentGate.Application.Interface;
using PaymentGate.Domain.DTO;

namespace PaymentGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransferController : ControllerBase
    {
        private readonly ITransferInterface _transferInterface;

        public TransferController(ITransferInterface transferInterface)
        {
            _transferInterface = transferInterface;
        }

        [HttpPost]

        public async Task<IActionResult> TransferRequste([FromBody] TransferRequestDto request)
        {
            if (request == null)
                return BadRequest("Requet body is requried");

            try
            {
                var response = await _transferInterface.ExecuteTransferAsync(request);
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
