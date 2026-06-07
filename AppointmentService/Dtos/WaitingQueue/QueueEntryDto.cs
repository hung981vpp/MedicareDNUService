namespace AppointmentService.Dtos.WaitingQueue;

public sealed class QueueEntryDto
{
    public int QueueId { get; init; }

    public int AppointmentId { get; init; }

    public int PatientId { get; init; }

    public string PatientName { get; init; } = string.Empty;

    public string PatientPhone { get; init; } = string.Empty;

    public int DoctorId { get; init; }

    public string DoctorName { get; init; } = string.Empty;

    public string SpecialtyName { get; init; } = string.Empty;

    public DateOnly AppointmentDate { get; init; }

    public TimeOnly SlotTime { get; init; }

    public DateOnly QueueDate { get; init; }

    public int QueueNumber { get; init; }

    public string Reason { get; init; } = string.Empty;

    public string AppointmentStatus { get; init; } = string.Empty;

    public string QueueStatus { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}
