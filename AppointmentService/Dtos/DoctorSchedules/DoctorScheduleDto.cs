namespace AppointmentService.Dtos.DoctorSchedules;

public sealed class DoctorScheduleDto
{
    public int ScheduleId { get; init; }

    public int DoctorId { get; init; }

    public string DoctorName { get; init; } = string.Empty;

    public DateOnly WorkDate { get; init; }

    public TimeOnly StartTime { get; init; }

    public TimeOnly EndTime { get; init; }

    public int SlotDurationMinutes { get; init; }

    public bool IsAvailable { get; init; }
}
