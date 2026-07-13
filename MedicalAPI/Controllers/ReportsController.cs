using MedicalAPI.Application.Common;
using MedicalAPI.Application.DTOs;
using MedicalAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalAPI.Controllers;

[Route("api/v1/medical/reports")]
[Authorize(Roles = "Admin")]
public sealed class ReportsController(MedicalDbContext context) : MedicalControllerBase
{
    [HttpGet("dashboard-summary")]
    [EndpointSummary("Tổng hợp báo cáo bệnh nhân mới")]
    [EndpointDescription("Đếm số hồ sơ bệnh nhân mới được tạo trong khoảng ngày phục vụ dashboard vận hành.")]
    public async Task<IActionResult> GetDashboardSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var range = ResolveDateRange(startDate, endDate);
        if (range.StartDate > range.EndDate)
        {
            var traceId = Request.Headers.TryGetValue("X-Request-Id", out var requestId) && !string.IsNullOrWhiteSpace(requestId)
                ? requestId.ToString()
                : HttpContext.TraceIdentifier;

            return BadRequest(ApiResponse<MedicalDashboardSummaryDto>.Fail(
                "endDate must be greater than or equal to startDate.",
                traceId,
                new ApiError("dateRange", "INVALID_RANGE", "endDate must be greater than or equal to startDate.")));
        }

        var endExclusive = range.EndDate.Date.AddDays(1);
        var count = await context.Patients
            .AsNoTracking()
            .CountAsync(patient => !patient.IsDeleted
                && patient.CreatedAt >= range.StartDate.Date
                && patient.CreatedAt < endExclusive);

        return ToActionResult(Result<MedicalDashboardSummaryDto>.Ok(
            new MedicalDashboardSummaryDto { NewPatientsCount = count },
            "Medical dashboard summary retrieved successfully."));
    }

    private static (DateTime StartDate, DateTime EndDate) ResolveDateRange(DateTime? startDate, DateTime? endDate)
    {
        var end = (endDate ?? DateTime.UtcNow).Date;
        var start = (startDate ?? end.AddDays(-29)).Date;
        return (start, end);
    }
}
