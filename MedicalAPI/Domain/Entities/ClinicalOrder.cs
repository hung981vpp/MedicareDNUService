using MedicalAPI.Domain.Constants;

namespace MedicalAPI.Domain.Entities;

public sealed class ClinicalOrder
{
    public int Id { get; set; }
    public string? ClinicalOrderCode { get; set; }
    public int MedicalRecordId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public string OrderName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string Status { get; set; } = MedicalStatuses.Ordered;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
