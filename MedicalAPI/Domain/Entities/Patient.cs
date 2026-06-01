using MedicalAPI.Domain.Constants;

namespace MedicalAPI.Domain.Entities;

public sealed class Patient
{
    public int Id { get; set; }
    public string? PatientCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? CitizenId { get; set; }
    public string? BloodType { get; set; }
    public string? AllergyNote { get; set; }
    public string? MedicalHistory { get; set; }
    public string Status { get; set; } = MedicalStatuses.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
