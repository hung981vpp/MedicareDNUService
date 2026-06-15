using System.ComponentModel.DataAnnotations;

namespace MedicalAPI.Application.DTOs;

public sealed class MedicalRecordCreateRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "VisitId phải lớn hơn 0")]
    public int VisitId { get; init; }

    [StringLength(50)]
    public string? DiagnosisCode { get; init; }

    [StringLength(100)]
    public string? DiagnosisSpecialty { get; init; }

    [Required(ErrorMessage = "Chẩn đoán không được để trống")]
    [StringLength(500)]
    public string DiagnosisText { get; init; } = string.Empty;

    public string? DoctorNote { get; init; }
    public string? TreatmentPlan { get; init; }
    public DateOnly? FollowUpDate { get; init; }
}

public sealed class MedicalRecordUpdateRequest
{
    [StringLength(50)]
    public string? DiagnosisCode { get; init; }

    [StringLength(100)]
    public string? DiagnosisSpecialty { get; init; }

    [Required(ErrorMessage = "Chẩn đoán không được để trống")]
    [StringLength(500)]
    public string DiagnosisText { get; init; } = string.Empty;

    public string? DoctorNote { get; init; }
    public string? TreatmentPlan { get; init; }
    public DateOnly? FollowUpDate { get; init; }
}

public sealed record MedicalRecordDetailDto(
    int Id,
    string? MedicalRecordCode,
    int VisitId,
    int PatientId,
    string? PatientCode,
    int DoctorId,
    string? DiagnosisCode,
    string? DiagnosisSpecialty,
    string DiagnosisText,
    string? DoctorNote,
    string? TreatmentPlan,
    DateOnly? FollowUpDate,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? CompletedAt);
