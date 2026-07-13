namespace AppointmentService.Dtos.Reports;

public sealed class AppointmentTrendDto
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}

public sealed class SpecialtyDistributionDto
{
    public string SpecialtyName { get; set; } = string.Empty;
    public int AppointmentCount { get; set; }
}

public sealed class AppointmentStatusRatioDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class AppointmentDashboardSummaryDto
{
    public int TotalAppointments { get; set; }
    public List<AppointmentTrendDto> AppointmentTrends { get; set; } = new();
    public List<SpecialtyDistributionDto> SpecialtyDistribution { get; set; } = new();
    public List<AppointmentStatusRatioDto> AppointmentStatusRatio { get; set; } = new();
}
