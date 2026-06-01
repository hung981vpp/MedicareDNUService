namespace AppointmentService.IntegrationEvents;

public sealed class AppointmentCompletedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public int AppointmentId { get; init; }

    public int PatientId { get; init; }

    public int DoctorId { get; init; }

    public DateOnly AppointmentDate { get; init; }

    public TimeOnly SlotTime { get; init; }

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
