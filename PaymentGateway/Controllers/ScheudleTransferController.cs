using Microsoft.AspNetCore.Mvc;
using PaymentGate.Application.DTO;
using PaymentGate.Application.Interface;
using static System.Net.Mime.MediaTypeNames;

namespace PaymentGateway.Controllers
{
    [Route("api/scheduled-transfers")]
    [ApiController]
    public class ScheduledTransferController : ControllerBase
    {
        private readonly IScheduleTransfer _scheduleTransfer;

        public ScheduledTransferController(IScheduleTransfer scheduleTransfer)
        {
            _scheduleTransfer = scheduleTransfer;
        }

        // POST api/scheduled-transfers
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ScheduledTransferRequestDto requestDto)
        {
            if (requestDto == null)
                return BadRequest(new { error = "Request body is required." });
            try
            {
                var response = await _scheduleTransfer.ScheduledTransferAsync(requestDto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET api/scheduled-transfers?initiatorId=xxx
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid initiatorId)
        {
            try
            {
                var response = await _scheduleTransfer.GetAllAsync(initiatorId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET api/scheduled-transfers/{id}?initiatorId=xxx
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, [FromQuery] Guid initiatorId)
        {
            try
            {
                var response = await _scheduleTransfer.GetByIdAsync(id, initiatorId);

                if (response == null)
                    return NotFound(new { error = "Scheduled transfer not found." });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // DELETE api/scheduled-transfers/{id}?initiatorId=xxx
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel([FromRoute] Guid id, [FromQuery] Guid initiatorId)
        {
            try
            {
                await _scheduleTransfer.CancelAsync(id, initiatorId);
                return Ok(new { message = "Scheduled transfer cancelled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}