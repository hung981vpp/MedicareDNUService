using System.ComponentModel.DataAnnotations;

namespace AppointmentService.Dtos.Doctors;

public sealed class UpdateDoctorRequest
{
    public int? UserId { get; init; }

    [MaxLength(120)]
    public string? FullName { get; init; }

    [MaxLength(120)]
    public string? DoctorName { get; init; }

    [Range(1, int.MaxValue)]
    public int SpecialtyId { get; init; }

    [MaxLength(80)]
    public string Degree { get; init; } = string.Empty;

    [Range(0, 80)]
    public int ExperienceYears { get; init; }

    [Range(0, 999999999)]
    public decimal ExamFee { get; init; }

    [MaxLength(20)]
    public string Phone { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(120)]
    public string Email { get; init; } = string.Empty;

    [MaxLength(20)]
    public string Gender { get; init; } = string.Empty;

    public DateOnly? DateOfBirth { get; init; }

    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    [MaxLength(500)]
    public string AvatarUrl { get; init; } = string.Empty;

    [MaxLength(30)]
    public string RoomNumber { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}
