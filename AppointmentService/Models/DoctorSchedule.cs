namespace AppointmentService.Models;

public sealed class DoctorSchedule
{
    public int Id { get; set; }

    public int DoctorId { get; set; }

    public DateOnly WorkDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int SlotDurationMinutes { get; set; } = 30;

    public bool IsAvailable { get; set; } = true;
}
