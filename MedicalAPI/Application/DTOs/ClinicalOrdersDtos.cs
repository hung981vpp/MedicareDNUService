using System.ComponentModel.DataAnnotations;

namespace MedicalAPI.Application.DTOs;

public sealed class ClinicalOrderCreateRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "MedicalRecordId phải lớn hơn 0")]
    public int MedicalRecordId { get; init; }

    [Required(ErrorMessage = "Loại chỉ định không được để trống")]
    [StringLength(50)]
    public string OrderType { get; init; } = string.Empty;

    [Required(ErrorMessage = "Tên chỉ định không được để trống")]
    [StringLength(200)]
    public string OrderName { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Reason { get; init; }
}

public sealed record ClinicalOrderDto(
    int Id,
    string? ClinicalOrderCode,
    int MedicalRecordId,
    int PatientId,
    int DoctorId,
    string OrderType,
    string OrderName,
    string? Reason,
    string Status,
    DateTime CreatedAt);
