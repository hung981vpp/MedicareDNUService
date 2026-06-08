using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Security;
using PharmacyBillingService.Services;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IBillingService _billingService;
        private readonly IConfiguration _configuration;

        public PaymentsController(IBillingService billingService, IConfiguration configuration)
        {
            _billingService = billingService;
            _configuration = configuration;
        }

        [HttpPost("sepay/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleSePayWebhook([FromBody] SePayWebhookDto webhookDto)
        {
            if (!IsValidSePayWebhook())
            {
                return Unauthorized(new { success = false, message = "Webhook khong hop le." });
            }

            try
            {
                await _billingService.PayInvoiceFromSePayWebhookAsync(webhookDto);
                return Ok(new { success = true });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("webhook-logs")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> GetWebhookLogs([FromQuery] string? status)
        {
            return Ok(await _billingService.GetPaymentWebhookLogsAsync(status));
        }

        private bool IsValidSePayWebhook()
        {
            var secret = _configuration["SePay:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                return true;
            }

            var headerName = _configuration["SePay:SignatureHeaderName"] ?? "X-SePay-Secret";
            var provided = Request.Headers[headerName].ToString();
            return string.Equals(provided, secret, StringComparison.Ordinal);
        }
    }
}
