namespace AppointmentService.Dtos.WaitingQueue;

public sealed class QueueEntryDto
{
    public int QueueId { get; init; }

    public int AppointmentId { get; init; }

    public int PatientId { get; init; }

    public int DoctorId { get; init; }

    public DateOnly QueueDate { get; init; }

    public int QueueNumber { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}
