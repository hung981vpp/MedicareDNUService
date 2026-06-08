using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyBillingService.Data;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Security;
using PharmacyBillingService.Services;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/invoices")]
    public class InvoicesController : ControllerBase
    {
        private readonly IBillingService _billingService;
        private readonly PharmacyDbContext _context;

        public InvoicesController(IBillingService billingService, PharmacyDbContext context)
        {
            _billingService = billingService;
            _context = context;
        }

        [HttpPost]
        [Authorize(Roles = RoleConstants.AdminOrNurse)]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto createDto)
        {
            try
            {
                var result = await _billingService.CreateInvoiceAsync(createDto);
                return CreatedAtAction(nameof(GetInvoiceById), new { id = result.InvoiceId }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = RoleConstants.AdminOrNurse)]
        public async Task<IActionResult> GetAllInvoices([FromQuery] string? status)
        {
            var result = await _billingService.GetAllInvoicesAsync(status);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = RoleConstants.StaffOrPatient)]
        public async Task<IActionResult> GetInvoiceById(int id)
        {
            var invoice = await _billingService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound(new { Message = "Khong tim thay hoa don yeu cau." });

            if (User.IsInRole(RoleConstants.Patient) && await GetCurrentPatientIdAsync() != invoice.PatientId)
            {
                return Forbid();
            }

            return Ok(invoice);
        }

        [HttpGet("patient/{patientKey}")]
        [Authorize(Roles = RoleConstants.AdminOrNurse + "," + RoleConstants.Patient)]
        public async Task<IActionResult> GetInvoicesByPatient(string patientKey)
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

            var result = await _billingService.GetInvoicesByPatientIdAsync(patientId);
            return Ok(result);
        }

        [HttpPost("{id}/pay")]
        [Authorize(Roles = RoleConstants.StaffOrPatient)]
        public async Task<IActionResult> PayInvoice(int id, [FromBody] PayInvoiceDto payDto)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            try
            {
                if (User.IsInRole(RoleConstants.Patient))
                {
                    var invoicePatientId = await _context.Invoices
                        .AsNoTracking()
                        .Where(i => i.InvoiceId == id)
                        .Select(i => (int?)i.PatientId)
                        .FirstOrDefaultAsync();

                    if (invoicePatientId is null) return NotFound(new { Message = "Khong tim thay hoa don yeu cau." });
                    if (await GetCurrentPatientIdAsync() != invoicePatientId) return Forbid();
                }

                var result = await _billingService.PayInvoiceAsync(id, payDto, userId.Value);
                return Ok(result);
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

        [HttpPost("{id}/refund")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> RefundInvoice(int id, [FromBody] RefundInvoiceDto refundDto)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            try
            {
                var result = await _billingService.RefundInvoiceAsync(id, refundDto, userId.Value);
                return Ok(result);
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

        [HttpPut("{id}/cancel")]
        [Authorize(Roles = RoleConstants.AdminOrNurse)]
        public async Task<IActionResult> CancelInvoice(int id)
        {
            try
            {
                var success = await _billingService.CancelInvoiceAsync(id);
                return success ? Ok(new { Message = "Huy hoa don thanh cong." }) : NotFound(new { Message = "Khong tim thay hoa don yeu cau." });
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
