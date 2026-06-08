using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyBillingService.Events;
using PharmacyBillingService.Security;
using PharmacyBillingService.Services;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/events")]
    [Authorize(Roles = RoleConstants.Admin)] // Enforce JWT/service authorization for N3 event ingress
    public class EventsController : ControllerBase
    {
        private readonly IPrescriptionService _prescriptionService;

        public EventsController(IPrescriptionService prescriptionService)
        {
            _prescriptionService = prescriptionService;
        }

        /// <summary>
        /// Tiêu thụ Event prescription.created thật từ Medical Record Service (N2) qua Outbox.
        /// </summary>
        [HttpPost("prescription-created")]
        public async Task<IActionResult> ProcessPrescriptionCreated([FromBody] PrescriptionCreatedEvent ev)
        {
            try
            {
                if (ev == null)
                {
                    return BadRequest(new { Message = "Payload sự kiện không hợp lệ." });
                }

                // If CreatedAt is not populated, map from OccurredAt
                if (ev.CreatedAt == default && ev.OccurredAt != default)
                {
                    ev.CreatedAt = ev.OccurredAt;
                }

                var result = await _prescriptionService.ProcessPrescriptionCreatedEventAsync(ev);
                return Ok(new
                {
                    Message = "Xử lý sự kiện prescription.created thành công.",
                    Prescription = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi tiêu thụ sự kiện.", Detail = ex.Message });
            }
        }
    }
}
