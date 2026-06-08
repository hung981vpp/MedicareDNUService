using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyBillingService.Security;
using PharmacyBillingService.Services;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("revenue/daily")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> GetDailyRevenue([FromQuery] int days = 30)
        {
            return Ok(await _reportService.GetDailyRevenueAsync(days));
        }

        [HttpGet("revenue/monthly")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> GetMonthlyRevenue([FromQuery] int months = 12)
        {
            return Ok(await _reportService.GetMonthlyRevenueAsync(months));
        }

        [HttpGet("top-medicines")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> GetTopMedicines([FromQuery] int count = 5)
        {
            return Ok(await _reportService.GetTopMedicinesAsync(count));
        }

        [HttpGet("unpaid-invoices")]
        [Authorize(Roles = RoleConstants.AdminOrNurse)]
        public async Task<IActionResult> GetUnpaidInvoices()
        {
            return Ok(await _reportService.GetUnpaidInvoicesAsync());
        }

        [HttpGet("low-stock")]
        [Authorize(Roles = RoleConstants.AdminOrPharmacist)]
        public async Task<IActionResult> GetLowStockReport()
        {
            return Ok(await _reportService.GetLowStockReportAsync());
        }
    }
}
