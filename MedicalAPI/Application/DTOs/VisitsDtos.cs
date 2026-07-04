using System.ComponentModel.DataAnnotations;

namespace MedicalAPI.Application.DTOs;

public sealed class VisitCreateRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "AppointmentId phải lớn hơn 0")]
    public int? AppointmentId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "PatientId phải lớn hơn 0")]
    public int PatientId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "DoctorId phải lớn hơn 0")]
    public int DoctorId { get; init; }

    [StringLength(500)]
    public string? ChiefComplaint { get; init; }

    public string? Symptoms { get; init; }
}

public sealed class VisitStartRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "DoctorId phải lớn hơn 0")]
    public int DoctorId { get; init; }

    [Required(ErrorMessage = "Lý do khám không được để trống")]
    [StringLength(500)]
    public string ChiefComplaint { get; init; } = string.Empty;
}

public sealed class VisitVitalsRequest
{
    [Range(30, 45, ErrorMessage = "Nhiệt độ phải nằm trong khoảng 30 đến 45")]
    public decimal? Temperature { get; init; }

    [StringLength(30)]
    [RegularExpression(@"^\d{2,3}\s*/\s*\d{2,3}$", ErrorMessage = "Huyết áp phải có dạng tâm thu/tâm trương, ví dụ 120/80")]
    public string? BloodPressure { get; init; }

    [Range(1, 250, ErrorMessage = "Nhịp tim phải lớn hơn 0")]
    public int? HeartRate { get; init; }

    [Range(1, 100, ErrorMessage = "Nhịp thở phải nằm trong khoảng 1 đến 100")]
    public int? RespiratoryRate { get; init; }

    [Range(1, 100, ErrorMessage = "SpO2 phải nằm trong khoảng 1 đến 100")]
    public int? Spo2 { get; init; }

    [Range(1, 500, ErrorMessage = "Cân nặng phải lớn hơn 0")]
    public decimal? Weight { get; init; }

    [Range(1, 300, ErrorMessage = "Chiều cao phải lớn hơn 0")]
    public decimal? Height { get; init; }

    [StringLength(2000)]
    public string? Note { get; init; }
}

public sealed class VisitCancelRequest
{
    [Required(ErrorMessage = "Lý do hủy không được để trống")]
    [StringLength(500)]
    public string CancelReason { get; init; } = string.Empty;
}

public sealed record VisitDetailDto(
    int Id,
    string? VisitCode,
    int? AppointmentId,
    int PatientId,
    string? PatientCode,
    string PatientName,
    int DoctorId,
    string? DoctorName,
    DateTime VisitDate,
    string? ChiefComplaint,
    string? Symptoms,
    string? VitalSignsJson,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? CancelReason);
