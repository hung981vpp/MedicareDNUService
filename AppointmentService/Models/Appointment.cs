using AppointmentService.Constants;

namespace AppointmentService.Models;

public sealed class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string PatientNameSnapshot { get; set; } = string.Empty;

    public string PatientPhoneSnapshot { get; set; } = string.Empty;

    public int DoctorId { get; set; }

    public DateOnly AppointmentDate { get; set; }

    public TimeOnly SlotTime { get; set; }

    public string Reason { get; set; } = string.Empty;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    public int? QueueNumber { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
