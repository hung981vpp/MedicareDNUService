namespace AppointmentService.Dtos.Integration;

public sealed class AppointmentForMedicalDto
{
    public int AppointmentId { get; init; }

    public int PatientId { get; init; }

    public string PatientName { get; init; } = string.Empty;

    public string PatientPhone { get; init; } = string.Empty;

    public int DoctorId { get; init; }

    public string DoctorName { get; init; } = string.Empty;

    public int SpecialtyId { get; init; }

    public string SpecialtyName { get; init; } = string.Empty;

    public DateOnly AppointmentDate { get; init; }

    public TimeOnly SlotTime { get; init; }

    public string Status { get; init; } = string.Empty;

    public int? QueueNumber { get; init; }
}
