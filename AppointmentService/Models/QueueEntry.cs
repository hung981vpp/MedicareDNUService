using AppointmentService.Constants;

namespace AppointmentService.Models;

public sealed class QueueEntry
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public DateOnly QueueDate { get; set; }

    public int QueueNumber { get; set; }

    public QueueStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
