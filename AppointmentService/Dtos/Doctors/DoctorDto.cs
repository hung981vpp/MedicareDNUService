namespace AppointmentService.Dtos.Doctors;

public sealed class DoctorDto
{
    public int DoctorId { get; init; }

    public int? UserId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string DoctorName => FullName;

    public int SpecialtyId { get; init; }

    public string SpecialtyName { get; init; } = string.Empty;

    public string Degree { get; init; } = string.Empty;

    public int ExperienceYears { get; init; }

    public decimal ExamFee { get; init; }

    public string Phone { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Gender { get; init; } = string.Empty;

    public DateOnly? DateOfBirth { get; init; }

    public string Description { get; init; } = string.Empty;

    public string AvatarUrl { get; init; } = string.Empty;

    public string RoomNumber { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
