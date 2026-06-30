using MedicalAPI.Application.Common;
using MedicalAPI.Application.DTOs;

namespace MedicalAPI.Application.Services;

public interface IMedicalRecordService
{
    Result<PagedList<PatientDetailDto>> SearchPatients(string? keyword, int pageNumber, int pageSize);
    Result<IReadOnlyList<PatientLookupDto>> LookupPatientsForBooking(string? keyword, int limit);
    Result<PatientDetailDto> GetPatient(int id);
    Result<PatientDetailDto> GetPatientByKey(string patientKey, int? currentUserId, int? currentPatientId, string? currentEmail, string? currentFullName);
    Result<PatientDetailDto> CreatePatient(PatientCreateRequest request);
    Result<PatientDetailDto> UpdatePatient(int id, PatientUpdateRequest request);
    Result<bool> DeletePatient(int id);
    Result<PatientDetailDto> UpdateCurrentPatient(PatientUpdateRequest request);
    Result<PatientHistoryDto> GetPatientHistory(int id);
    Result<PatientHistoryDto> GetPatientHistoryByKey(string patientKey, int? currentUserId, int? currentPatientId, string? currentEmail, string? currentFullName);
    Result<PatientDetailDto> GetCurrentPatient();
    Result<PatientHistoryDto> GetCurrentPatientHistory();
    Result<PatientClinicalTimelineDto> GetCurrentPatientClinicalTimeline();
    Result<PatientClinicalTimelineDto> GetPatientClinicalTimeline(int patientId);

    Result<IReadOnlyList<VisitDetailDto>> GetTodayVisits(int? doctorId);
    Result<VisitDetailDto> GetVisit(int id);
    Result<VisitDetailDto> GetVisitByAppointment(int appointmentId);
    Result<VisitDetailDto> CreateVisit(VisitCreateRequest request);
    Result<VisitDetailDto> StartVisit(int id, VisitStartRequest request);
    Result<VisitDetailDto> UpdateVitals(int id, VisitVitalsRequest request);
    Result<VisitDetailDto> CompleteVisit(int id);
    Result<VisitDetailDto> CancelVisit(int id, VisitCancelRequest request);

    Result<MedicalRecordDetailDto> CreateMedicalRecord(MedicalRecordCreateRequest request);
    Result<MedicalRecordDetailDto> GetMedicalRecord(int id);
    Result<MedicalRecordDetailDto> GetMedicalRecordByVisit(int visitId);
    Result<MedicalRecordDetailDto> UpdateMedicalRecord(int id, MedicalRecordUpdateRequest request);
    Result<MedicalRecordDetailDto> CompleteMedicalRecord(int id);
    Result<CompleteMedicalRecordDto> GetCompleteMedicalRecord(int id);
    Result<string> ExportMedicalRecordHtml(int id);

    Result<PrescriptionDetailDto> CreatePrescription(PrescriptionCreateRequest request);
    Result<PrescriptionDetailDto> GetPrescription(int id);
    Result<PrescriptionDetailDto> AddPrescriptionItem(int id, PrescriptionItemRequest request);
    Result<PrescriptionDetailDto> UpdatePrescriptionItem(int id, int itemId, PrescriptionItemRequest request);
    Result<PrescriptionDetailDto> DeletePrescriptionItem(int id, int itemId);
    Result<PrescriptionSubmitDto> SubmitPrescription(int id, PrescriptionSubmitRequest? request);
    Result<PrescriptionDetailDto> CancelPrescription(int id, PrescriptionCancelRequest request);
    Result<IReadOnlyList<MedicineCatalogDto>> GetMedicineCatalog(string? name, string? activeIngredient, string? status);

    Result<ClinicalOrderDto> CreateClinicalOrder(ClinicalOrderCreateRequest request);
    Result<IReadOnlyList<ClinicalOrderDto>> GetClinicalOrders(int? medicalRecordId, int? patientId);
    Result<ClinicalOrderDto> UpdateClinicalOrderResult(int id, ClinicalOrderResultRequest request);

    Result<EventResultDto> HandleAppointmentConfirmed(AppointmentConfirmedEventRequest request);
    Result<EventResultDto> HandlePatientCheckedIn(PatientCheckedInEventRequest request);
    Result<IReadOnlyList<InboxEventDto>> GetInboxEvents(string? status, string? eventType);
    Result<IReadOnlyList<OutboxEventDto>> GetOutboxEvents(string? status, string? eventType);
    Result<OutboxEventDto> MarkOutboxPublished(int id);
    Result<OutboxEventDto> RetryOutboxEvent(int id);
    Result<OutboxEventDto> FailOutboxEvent(int id);
}
