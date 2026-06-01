using System.ComponentModel.DataAnnotations;

namespace AppointmentService.Dtos.Appointments;

public sealed class CreateAppointmentRequest
{
    [Range(1, int.MaxValue)]
    public int PatientId { get; init; }

    [Required]
    [MaxLength(120)]
    public string PatientNameSnapshot { get; init; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PatientPhoneSnapshot { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int DoctorId { get; init; }

    public DateOnly AppointmentDate { get; init; }

    public TimeOnly SlotTime { get; init; }

    [MaxLength(500)]
    public string Reason { get; init; } = string.Empty;
}
