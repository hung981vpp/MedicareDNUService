using System.ComponentModel.DataAnnotations;

namespace MedicalAPI.Application.DTOs;

public sealed class PatientCreateRequest
{
    [Required(ErrorMessage = "Họ tên không được để trống")]
    [StringLength(150, ErrorMessage = "Họ tên không được vượt quá 150 ký tự")]
    public string FullName { get; init; } = string.Empty;

    public DateOnly? DateOfBirth { get; init; }

    [StringLength(20)]
    public string? Gender { get; init; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20)]
    public string? PhoneNumber { get; init; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(150)]
    public string? Email { get; init; }

    [StringLength(255)]
    public string? Address { get; init; }

    [StringLength(20)]
    public string? CitizenId { get; init; }

    [StringLength(10)]
    public string? BloodType { get; init; }

    public string? AllergyNote { get; init; }
    public string? MedicalHistory { get; init; }
}

public sealed class PatientUpdateRequest
{
    [Required(ErrorMessage = "Họ tên không được để trống")]
    [StringLength(150, ErrorMessage = "Họ tên không được vượt quá 150 ký tự")]
    public string FullName { get; init; } = string.Empty;

    public DateOnly? DateOfBirth { get; init; }

    [StringLength(20)]
    public string? Gender { get; init; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20)]
    public string? PhoneNumber { get; init; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(150)]
    public string? Email { get; init; }

    [StringLength(255)]
    public string? Address { get; init; }

    [StringLength(20)]
    public string? CitizenId { get; init; }

    [StringLength(10)]
    public string? BloodType { get; init; }

    public string? AllergyNote { get; init; }
    public string? MedicalHistory { get; init; }

    [StringLength(30)]
    public string? Status { get; init; }
}

public sealed record PatientSummaryDto(
    int Id,
    string? PatientCode,
    string FullName,
    string? PhoneNumber,
    string Status);

public sealed record PatientLookupDto(
    int Id,
    string? PatientCode,
    string FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string Status);

public sealed record PatientDetailDto(
    int Id,
    string? PatientCode,
    string FullName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? PhoneNumber,
    string? Email,
    string? Address,
    string? CitizenId,
    string? BloodType,
    string? AllergyNote,
    string? MedicalHistory,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record PatientHistoryDto(
    PatientDetailDto Patient,
    IReadOnlyList<VisitDetailDto> Visits,
    IReadOnlyList<MedicalRecordDetailDto> MedicalRecords,
    IReadOnlyList<PrescriptionDetailDto> Prescriptions);
