using MedicalAPI.Domain.Constants;

namespace MedicalAPI.Domain.Entities;

public sealed class MedicalRecord
{
    public int Id { get; set; }
    public string? MedicalRecordCode { get; set; }
    public int VisitId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public string? DiagnosisCode { get; set; }
    public string? DiagnosisSpecialty { get; set; }
    public string DiagnosisText { get; set; } = string.Empty;
    public string? DoctorNote { get; set; }
    public string? TreatmentPlan { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string Status { get; set; } = MedicalStatuses.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
