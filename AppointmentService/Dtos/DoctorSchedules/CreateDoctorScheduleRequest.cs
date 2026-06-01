using System.ComponentModel.DataAnnotations;

namespace AppointmentService.Dtos.DoctorSchedules;

public sealed class CreateDoctorScheduleRequest
{
    [Range(1, int.MaxValue)]
    public int DoctorId { get; init; }

    public DateOnly WorkDate { get; init; }

    public TimeOnly StartTime { get; init; }

    public TimeOnly EndTime { get; init; }

    [Range(5, 240)]
    public int SlotDurationMinutes { get; init; } = 30;

    public bool IsAvailable { get; init; } = true;
}
