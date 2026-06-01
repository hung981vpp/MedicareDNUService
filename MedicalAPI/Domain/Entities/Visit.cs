using MedicalAPI.Domain.Constants;

namespace MedicalAPI.Domain.Entities;

public sealed class Visit
{
    public int Id { get; set; }
    public string? VisitCode { get; set; }
    public int? AppointmentId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime VisitDate { get; set; } = DateTime.UtcNow;
    public string? ChiefComplaint { get; set; }
    public string? Symptoms { get; set; }
    public string? VitalSignsJson { get; set; }
    public string Status { get; set; } = MedicalStatuses.WaitingForExam;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CancelReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
