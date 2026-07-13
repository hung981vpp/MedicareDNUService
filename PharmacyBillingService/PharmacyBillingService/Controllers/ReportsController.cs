using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyBillingService.DTOs;
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

        [HttpGet("dashboard-summary")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> GetDashboardSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var range = ResolveDateRange(startDate, endDate);
            if (range.StartDate > range.EndDate)
            {
                return BadRequest(ApiResponse<PharmacyDashboardSummaryDto>.Fail("endDate must be greater than or equal to startDate."));
            }

            var data = await _reportService.GetDashboardSummaryAsync(range.StartDate, range.EndDate);
            return Ok(ApiResponse<PharmacyDashboardSummaryDto>.Ok(data, "Pharmacy dashboard summary retrieved successfully."));
        }

        private static (DateTime StartDate, DateTime EndDate) ResolveDateRange(DateTime? startDate, DateTime? endDate)
        {
            var end = (endDate ?? DateTime.UtcNow).Date;
            var start = (startDate ?? end.AddDays(-29)).Date;
            return (start, end);
        }
    }

    public sealed class ApiResponse<T>
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }
        public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        public static ApiResponse<T> Ok(T data, string message) => new()
        {
            Success = true,
            Message = message,
            Data = data
        };

        public static ApiResponse<T> Fail(string message, IReadOnlyList<string>? errors = null) => new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? Array.Empty<string>()
        };
    }
}
