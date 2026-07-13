using AppointmentService.Common;
using AppointmentService.Data;
using AppointmentService.Dtos.Reports;
using AppointmentService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Controllers;

[ApiController]
[Route("api/reports")]
[Tags("Reports")]
[Authorize(Roles = "Admin")]
public sealed class ReportsController : ControllerBase
{
    private readonly AppointmentDbContext _context;

    public ReportsController(AppointmentDbContext context)
    {
        _context = context;
    }

    [HttpGet("dashboard-summary")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDashboardSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDashboardSummaryDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AppointmentDashboardSummaryDto>>> GetDashboardSummary(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate)
    {
        var range = ResolveDateRange(startDate, endDate);
        if (range.StartDate > range.EndDate)
        {
            return BadRequest(ApiResponse<AppointmentDashboardSummaryDto>.Fail("endDate must be greater than or equal to startDate."));
        }

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDate >= range.StartDate && a.AppointmentDate <= range.EndDate)
            .ToListAsync();

        var specialtyDistribution = await (
                from appointment in _context.Appointments.AsNoTracking()
                join doctor in _context.Doctors.AsNoTracking() on appointment.DoctorId equals doctor.Id
                join specialty in _context.Specialties.AsNoTracking() on doctor.SpecialtyId equals specialty.Id
                where appointment.AppointmentDate >= range.StartDate && appointment.AppointmentDate <= range.EndDate
                group appointment by specialty.Name into specialtyGroup
                select new SpecialtyDistributionDto
                {
                    SpecialtyName = specialtyGroup.Key,
                    AppointmentCount = specialtyGroup.Count()
                })
            .OrderByDescending(item => item.AppointmentCount)
            .ToListAsync();

        var data = new AppointmentDashboardSummaryDto
        {
            TotalAppointments = appointments.Count,
            AppointmentTrends = appointments
                .GroupBy(a => a.AppointmentDate)
                .Select(g => new AppointmentTrendDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(item => item.Date)
                .ToList(),
            SpecialtyDistribution = specialtyDistribution,
            AppointmentStatusRatio = appointments
                .GroupBy(a => a.Status)
                .Select(g => new AppointmentStatusRatioDto
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .OrderByDescending(item => item.Count)
                .ToList()
        };

        return Ok(ApiResponse<AppointmentDashboardSummaryDto>.Ok(data, "Appointment dashboard summary retrieved successfully."));
    }

    private static (DateOnly StartDate, DateOnly EndDate) ResolveDateRange(DateOnly? startDate, DateOnly? endDate)
    {
        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = startDate ?? end.AddDays(-29);
        return (start, end);
    }
}
