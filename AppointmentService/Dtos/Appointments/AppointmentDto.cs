namespace AppointmentService.Dtos.Appointments;

public sealed class AppointmentDto
{
    public int AppointmentId { get; init; }

    public int PatientId { get; init; }

    public string PatientName { get; init; } = string.Empty;

    public string PatientPhone { get; init; } = string.Empty;

    public int DoctorId { get; init; }

    public string DoctorName { get; init; } = string.Empty;

    public int SpecialtyId { get; init; }

    public string SpecialtyName { get; init; } = string.Empty;

    public decimal ExamFee { get; init; }

    public DateOnly AppointmentDate { get; init; }

    public TimeOnly SlotTime { get; init; }

    public string Reason { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int? QueueNumber { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
