namespace MedicalAPI.Application.DTOs;

public sealed record AppointmentSnapshotDto(
    int AppointmentId,
    int? PatientId,
    string PatientNameSnapshot,
    int DoctorId,
    string? DoctorNameSnapshot,
    int? SpecialtyId,
    string? SpecialtyNameSnapshot,
    DateTime ScheduledAt,
    int? QueueNumber,
    string Status,
    DateTime? ConfirmedAt,
    DateTime CreatedAt);

public sealed record BillingSummaryDto(
    string Source,
    string Status,
    string Message,
    int? InvoiceId,
    decimal? TotalAmount,
    string? InvoiceStatus);

public sealed record ClinicalVisitBundleDto(
    AppointmentSnapshotDto? Appointment,
    VisitDetailDto Visit,
    MedicalRecordDetailDto? MedicalRecord,
    IReadOnlyList<ClinicalOrderDto> ClinicalOrders,
    IReadOnlyList<PrescriptionDetailDto> Prescriptions,
    BillingSummaryDto Billing);

public sealed record PatientClinicalTimelineDto(
    PatientDetailDto Patient,
    IReadOnlyList<ClinicalVisitBundleDto> Visits);

public sealed record CompleteMedicalRecordDto(
    PatientDetailDto Patient,
    AppointmentSnapshotDto? Appointment,
    VisitDetailDto Visit,
    MedicalRecordDetailDto MedicalRecord,
    IReadOnlyList<ClinicalOrderDto> ClinicalOrders,
    IReadOnlyList<PrescriptionDetailDto> Prescriptions,
    BillingSummaryDto Billing);
