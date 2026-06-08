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
    public string? ResultText { get; set; }
    public string? ResultValue { get; set; }
    public string? ResultUnit { get; set; }
    public string? ResultFileUrl { get; set; }
    public string? Conclusion { get; set; }
    public DateTime? ResultedAt { get; set; }
    public string? ResultedBy { get; set; }
}
