using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PharmacyBillingService.Data;
using PharmacyBillingService.Security;
using PharmacyBillingService.Services;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/prescriptions")]
    public class PrescriptionsController : ControllerBase
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly PharmacyDbContext _context;
        private readonly ILogger<PrescriptionsController> _logger;

        public PrescriptionsController(IPrescriptionService prescriptionService, PharmacyDbContext context, ILogger<PrescriptionsController> logger)
        {
            _prescriptionService = prescriptionService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = RoleConstants.Staff)]
        public async Task<IActionResult> GetAllPrescriptions([FromQuery] string? status)
        {
            try
            {
                return Ok(await _prescriptionService.GetAllPrescriptionsAsync(status));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business error while loading prescriptions.");
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while loading prescriptions.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Khong tai duoc danh sach don thuoc.", Detail = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = RoleConstants.StaffOrPatient)]
        public async Task<IActionResult> GetPrescriptionById(int id)
        {
            var prescription = await _prescriptionService.GetPrescriptionByIdAsync(id);
            if (prescription == null) return NotFound(new { Message = "Khong tim thay don thuoc yeu cau." });

            if (User.IsInRole(RoleConstants.Patient) && await GetCurrentPatientIdAsync() != prescription.PatientId)
            {
                return Forbid();
            }

            return Ok(prescription);
        }

        [HttpGet("{id}/stock-check")]
        [Authorize(Roles = RoleConstants.Staff)]
        public async Task<IActionResult> CheckStock(int id)
        {
            try
            {
                return Ok(await _prescriptionService.CheckPrescriptionStockAsync(id));
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = RoleConstants.AdminOrPharmacist)]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                return Ok(await _prescriptionService.ApprovePrescriptionAsync(id));
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("patient/{patientKey}")]
        [Authorize(Roles = RoleConstants.StaffOrPatient)]
        public async Task<IActionResult> GetPrescriptionsByPatient(string patientKey)
        {
            if (!TryResolvePatientKey(patientKey, out var patientId))
            {
                return BadRequest(new { Message = "Ma benh nhan khong hop le." });
            }

            if (User.IsInRole(RoleConstants.Patient))
            {
                var allowedPatientId = await GetCurrentPatientIdAsync();
                if (allowedPatientId is null) return Forbid();
                patientId = allowedPatientId.Value;
            }

            return Ok(await _prescriptionService.GetPrescriptionsByPatientIdAsync(patientId));
        }

        [HttpPost("{id}/dispense")]
        [Authorize(Roles = RoleConstants.AdminOrPharmacist)]
        public async Task<IActionResult> Dispense(int id)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            try
            {
                var success = await _prescriptionService.DispensePrescriptionAsync(id, userId.Value);
                return success ? Ok(new { Message = "Xuat thuoc thanh cong va da tru ton kho." }) : BadRequest(new { Message = "Xuat thuoc khong thanh cong." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        private static bool TryResolvePatientKey(string patientKey, out int patientId)
        {
            if (int.TryParse(patientKey, out patientId)) return true;
            if (patientKey.StartsWith("BN", StringComparison.OrdinalIgnoreCase) && int.TryParse(patientKey[2..], out patientId)) return true;
            patientId = 0;
            return false;
        }

        private int? GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }

        private async Task<int?> GetCurrentPatientIdAsync()
        {
            var claimValue = User.FindFirst("PatientId")?.Value;
            if (int.TryParse(claimValue, out var claimPatientId)) return claimPatientId;

            var userId = GetCurrentUserId();
            if (userId is null) return null;

            return await _context.Users.AsNoTracking()
                .Where(u => u.UserId == userId.Value)
                .Select(u => u.PatientId)
                .FirstOrDefaultAsync();
        }
    }
}
