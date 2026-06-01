namespace AppointmentService.Models;

public sealed class Doctor
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public int SpecialtyId { get; set; }

    public string Degree { get; set; } = string.Empty;

    public int ExperienceYears { get; set; }

    public decimal ExamFee { get; set; }

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public string Description { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
