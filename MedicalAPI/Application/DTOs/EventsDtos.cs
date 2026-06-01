using System.ComponentModel.DataAnnotations;

namespace MedicalAPI.Application.DTOs;

public sealed class AppointmentConfirmedEventRequest
{
    [Required(ErrorMessage = "EventCode không được để trống")]
    [StringLength(30)]
    public string EventCode { get; init; } = string.Empty;

    [Required(ErrorMessage = "EventType không được để trống")]
    [StringLength(100)]
    public string EventType { get; init; } = string.Empty;

    [Required(ErrorMessage = "Source không được để trống")]
    [StringLength(100)]
    public string Source { get; init; } = string.Empty;

    public DateTime OccurredAt { get; init; }

    [Required(ErrorMessage = "Data không được để trống")]
    public AppointmentConfirmedData Data { get; init; } = new();
}

public sealed class AppointmentConfirmedData
{
    [Range(1, int.MaxValue, ErrorMessage = "AppointmentId phải lớn hơn 0")]
    public int AppointmentId { get; init; }

    [Required(ErrorMessage = "Tên bệnh nhân không được để trống")]
    [StringLength(150)]
    public string PatientName { get; init; } = string.Empty;

    public DateOnly? DateOfBirth { get; init; }

    [StringLength(20)]
    public string? Gender { get; init; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20)]
    public string? PhoneNumber { get; init; }

    [StringLength(20)]
    public string? CitizenId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "DoctorId phải lớn hơn 0")]
    public int DoctorId { get; init; }

    [StringLength(150)]
    public string? DoctorName { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "SpecialtyId phải lớn hơn 0")]
    public int? SpecialtyId { get; init; }

    [StringLength(150)]
    public string? SpecialtyName { get; init; }

    public DateTime ScheduledAt { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Số thứ tự phải lớn hơn 0")]
    public int? QueueNumber { get; init; }

    [Required(ErrorMessage = "Trạng thái không được để trống")]
    [StringLength(30)]
    public string Status { get; init; } = string.Empty;
}

public sealed class PatientCheckedInEventRequest
{
    [Required(ErrorMessage = "EventCode không được để trống")]
    [StringLength(30)]
    public string EventCode { get; init; } = string.Empty;

    [Required(ErrorMessage = "EventType không được để trống")]
    [StringLength(100)]
    public string EventType { get; init; } = string.Empty;

    [Required(ErrorMessage = "Source không được để trống")]
    [StringLength(100)]
    public string Source { get; init; } = string.Empty;

    public DateTime OccurredAt { get; init; }

    [Required(ErrorMessage = "Data không được để trống")]
    public PatientCheckedInData Data { get; init; } = new();
}

public sealed class PatientCheckedInData
{
    [Range(1, int.MaxValue, ErrorMessage = "AppointmentId phải lớn hơn 0")]
    public int AppointmentId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "DoctorId phải lớn hơn 0")]
    public int DoctorId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Số thứ tự phải lớn hơn 0")]
    public int? QueueNumber { get; init; }

    public DateTime CheckedInAt { get; init; }

    [Required(ErrorMessage = "Trạng thái không được để trống")]
    [StringLength(30)]
    public string Status { get; init; } = string.Empty;
}

public sealed record EventResultDto(string EventCode, string EventType, string Status, string Message);

public sealed record OutboxEventDto(
    int Id,
    string? EventCode,
    string EventType,
    string AggregateType,
    int AggregateId,
    string Payload,
    string Status,
    DateTime OccurredAt,
    DateTime? PublishedAt,
    int RetryCount);
