namespace MedicalAPI.Domain.Entities;

public sealed class AppointmentSnapshot
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int? PatientId { get; set; }
    public string PatientNameSnapshot { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string? DoctorNameSnapshot { get; set; }
    public int? SpecialtyId { get; set; }
    public string? SpecialtyNameSnapshot { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int? QueueNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
