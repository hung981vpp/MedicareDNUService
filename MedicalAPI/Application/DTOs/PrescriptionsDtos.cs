using System.ComponentModel.DataAnnotations;

namespace MedicalAPI.Application.DTOs;

public sealed class PrescriptionCreateRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "MedicalRecordId phải lớn hơn 0")]
    public int MedicalRecordId { get; init; }

    public string? Note { get; init; }
}

public sealed class PrescriptionSubmitRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "MedicalRecordId phải lớn hơn 0")]
    public int? MedicalRecordId { get; init; }

    public string? Note { get; init; }

    [Required(ErrorMessage = "Đơn thuốc phải có ít nhất một loại thuốc")]
    [MinLength(1, ErrorMessage = "Đơn thuốc phải có ít nhất một loại thuốc")]
    public IReadOnlyList<PrescriptionItemRequest> Items { get; init; } = [];
}

public sealed class PrescriptionItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "MedicineId phải lớn hơn 0")]
    public int MedicineId { get; init; }

    [Required(ErrorMessage = "Tên thuốc không được để trống")]
    [StringLength(200)]
    public string MedicineNameSnapshot { get; init; } = string.Empty;

    [StringLength(50)]
    public string? UnitSnapshot { get; init; }

    [Required(ErrorMessage = "Liều dùng không được để trống")]
    [StringLength(100)]
    public string Dosage { get; init; } = string.Empty;

    [Required(ErrorMessage = "Tần suất dùng không được để trống")]
    [StringLength(100)]
    public string Frequency { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Số ngày dùng phải lớn hơn 0")]
    public int DurationDays { get; init; }

    [Range(typeof(decimal), "0.01", "99999999.99", ErrorMessage = "Số lượng phải lớn hơn 0")]
    public decimal Quantity { get; init; }

    [StringLength(500)]
    public string? UsageInstruction { get; init; }

    [StringLength(500)]
    public string? Note { get; init; }
}

public sealed class PrescriptionCancelRequest
{
    [Required(ErrorMessage = "Lý do hủy không được để trống")]
    [StringLength(500)]
    public string CancelReason { get; init; } = string.Empty;
}

public sealed record PrescriptionItemDto(
    int Id,
    string? PrescriptionItemCode,
    int MedicineId,
    string MedicineNameSnapshot,
    string? UnitSnapshot,
    string Dosage,
    string Frequency,
    int DurationDays,
    decimal Quantity,
    string? UsageInstruction,
    string? Note);

public sealed record PrescriptionDetailDto(
    int Id,
    string? PrescriptionCode,
    int MedicalRecordId,
    string? MedicalRecordCode,
    int PatientId,
    string? PatientCode,
    int DoctorId,
    string Status,
    string? Note,
    DateTime CreatedAt,
    DateTime? SentToPharmacyAt,
    IReadOnlyList<PrescriptionItemDto> Items);

public sealed record PrescriptionSubmitDto(
    int Id,
    string? PrescriptionCode,
    int MedicalRecordId,
    string? MedicalRecordCode,
    string Status,
    string? EventCode);

public sealed record MedicineCatalogDto(
    int MedicineId,
    string MedicineName,
    string? ActiveIngredient,
    string Unit,
    decimal Price,
    int StockQuantity,
    int MinStockLevel,
    DateTime? ExpiryDate,
    string Status);
