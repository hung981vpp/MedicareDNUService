using MedicalAPI.Domain.Constants;

namespace MedicalAPI.Domain.Entities;

public sealed class Prescription
{
    public int Id { get; set; }
    public string? PrescriptionCode { get; set; }
    public int MedicalRecordId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public string Status { get; set; } = MedicalStatuses.Draft;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentToPharmacyAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
