namespace AppointmentService.Dtos.Integration;

public sealed class AppointmentEventDto
{
    public Guid EventId { get; init; }

    public string EventType { get; init; } = string.Empty;

    public int AppointmentId { get; init; }

    public int PatientId { get; init; }

    public int DoctorId { get; init; }

    public DateOnly AppointmentDate { get; init; }

    public TimeOnly SlotTime { get; init; }

    public string? Reason { get; init; }

    public DateTime OccurredAt { get; init; }
}
