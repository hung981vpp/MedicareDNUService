using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MedicalAPI.Application.Common;
using MedicalAPI.Application.DTOs;
using MedicalAPI.Domain.Constants;
using MedicalAPI.Domain.Entities;
using MedicalAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicalAPI.Application.Services;

public sealed class MedicalRecordService(
    MedicalDbContext db,
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    ILogger<MedicalRecordService> logger) : IMedicalRecordService
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public Result<PagedList<PatientSummaryDto>> SearchPatients(string? keyword, int pageNumber, int pageSize)
    {
        var normalized = keyword?.Trim();
        var patients = db.Patients
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Where(p => string.IsNullOrWhiteSpace(normalized)
                || p.FullName.Contains(normalized)
                || (p.PatientCode != null && p.PatientCode.Contains(normalized))
                || (p.PhoneNumber != null && p.PhoneNumber.Contains(normalized)))
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => ToSummary(p))
            .ToList();

        return Result<PagedList<PatientSummaryDto>>.Ok(
            PagedList<PatientSummaryDto>.Create(patients, pageNumber, pageSize),
            "Lấy danh sách bệnh nhân thành công");
    }

    public Result<PatientDetailDto> GetPatient(int id)
    {
        var patient = FindPatient(id);
        return patient is null
            ? NotFound<PatientDetailDto>("Không tìm thấy bệnh nhân")
            : Result<PatientDetailDto>.Ok(ToDetail(patient), "Lấy thông tin bệnh nhân thành công");
    }

    public Result<PatientSummaryDto> CreatePatient(PatientCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Invalid<PatientSummaryDto>("Dữ liệu không hợp lệ", "fullName", "REQUIRED", "Họ tên không được để trống");

        var patient = new Patient
        {
            FullName = request.FullName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Address = request.Address,
            CitizenId = request.CitizenId,
            BloodType = request.BloodType,
            AllergyNote = request.AllergyNote,
            MedicalHistory = request.MedicalHistory
        };

        db.Patients.Add(patient);
        db.SaveChanges();
        patient.PatientCode = $"BN{patient.Id:D3}";
        db.SaveChanges();

        return Result<PatientSummaryDto>.Ok(ToSummary(patient), "Tạo hồ sơ bệnh nhân thành công", StatusCodes.Status201Created);
    }

    public Result<PatientDetailDto> UpdatePatient(int id, PatientUpdateRequest request)
    {
        var patient = FindPatient(id);
        if (patient is null) return NotFound<PatientDetailDto>("Không tìm thấy bệnh nhân");
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Invalid<PatientDetailDto>("Dữ liệu không hợp lệ", "fullName", "REQUIRED", "Họ tên không được để trống");

        patient.FullName = request.FullName.Trim();
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = request.Gender;
        patient.PhoneNumber = request.PhoneNumber;
        patient.Email = request.Email;
        patient.Address = request.Address;
        patient.CitizenId = request.CitizenId;
        patient.BloodType = request.BloodType;
        patient.AllergyNote = request.AllergyNote;
        patient.MedicalHistory = request.MedicalHistory;
        patient.Status = string.IsNullOrWhiteSpace(request.Status) ? patient.Status : request.Status;
        patient.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();

        return Result<PatientDetailDto>.Ok(ToDetail(patient), "Cập nhật hồ sơ bệnh nhân thành công");
    }

    public Result<PatientHistoryDto> GetPatientHistory(int id)
    {
        var patient = FindPatient(id);
        if (patient is null) return NotFound<PatientHistoryDto>("Không tìm thấy bệnh nhân");

        var visits = db.Visits.AsNoTracking().Where(v => v.PatientId == id).ToList().Select(ToVisitDetail).ToList();
        var records = db.MedicalRecords.AsNoTracking().Where(r => r.PatientId == id).ToList().Select(ToMedicalRecordDetail).ToList();
        var prescriptions = db.Prescriptions.AsNoTracking().Where(p => p.PatientId == id).ToList().Select(ToPrescriptionDetail).ToList();

        return Result<PatientHistoryDto>.Ok(new(ToDetail(patient), visits, records, prescriptions), "Lấy lịch sử khám thành công");
    }

    public Result<IReadOnlyList<VisitDetailDto>> GetTodayVisits(int? doctorId)
    {
        var today = DateTime.UtcNow.Date;
        var visits = db.Visits
            .AsNoTracking()
            .Where(v => v.VisitDate >= today && v.VisitDate < today.AddDays(1))
            .Where(v => doctorId == null || v.DoctorId == doctorId)
            .OrderBy(v => v.VisitDate)
            .Select(v => ToVisitDetail(v))
            .ToList();

        return Result<IReadOnlyList<VisitDetailDto>>.Ok(visits, "Lấy danh sách lượt khám hôm nay thành công");
    }

    public Result<VisitDetailDto> GetVisit(int id)
    {
        var visit = db.Visits.AsNoTracking().FirstOrDefault(v => v.Id == id);
        return visit is null
            ? NotFound<VisitDetailDto>("Không tìm thấy lượt khám")
            : Result<VisitDetailDto>.Ok(ToVisitDetail(visit), "Lấy thông tin lượt khám thành công");
    }

    public Result<VisitDetailDto> GetVisitByAppointment(int appointmentId)
    {
        var visit = db.Visits.AsNoTracking().FirstOrDefault(v => v.AppointmentId == appointmentId);
        return visit is null
            ? NotFound<VisitDetailDto>("Không tìm thấy lượt khám tương ứng với lịch hẹn")
            : Result<VisitDetailDto>.Ok(ToVisitDetail(visit), "Lấy lượt khám theo lịch hẹn thành công");
    }

    public Result<VisitDetailDto> CreateVisit(VisitCreateRequest request)
    {
        if (FindPatient(request.PatientId) is null) return NotFound<VisitDetailDto>("Không tìm thấy bệnh nhân");
        if (request.AppointmentId.HasValue && db.Visits.Any(v => v.AppointmentId == request.AppointmentId))
            return Conflict<VisitDetailDto>("Lịch hẹn đã có lượt khám");

        var visit = new Visit
        {
            AppointmentId = request.AppointmentId,
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            ChiefComplaint = request.ChiefComplaint,
            Symptoms = request.Symptoms
        };

        db.Visits.Add(visit);
        db.SaveChanges();
        visit.VisitCode = $"LK{visit.Id:D3}";
        db.SaveChanges();

        return Result<VisitDetailDto>.Ok(ToVisitDetail(visit), "Tạo lượt khám thành công", StatusCodes.Status201Created);
    }

    public Result<VisitDetailDto> StartVisit(int id, VisitStartRequest request)
    {
        var visit = db.Visits.FirstOrDefault(v => v.Id == id);
        if (visit is null) return NotFound<VisitDetailDto>("Không tìm thấy lượt khám");
        if (visit.Status == MedicalStatuses.Cancelled) return Conflict<VisitDetailDto>("Lượt khám đã bị hủy");

        visit.DoctorId = request.DoctorId;
        visit.ChiefComplaint = request.ChiefComplaint;
        visit.Status = MedicalStatuses.InProgress;
        visit.StartedAt = DateTime.UtcNow;
        visit.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();

        return Result<VisitDetailDto>.Ok(ToVisitDetail(visit), "Bắt đầu khám thành công");
    }

    public Result<VisitDetailDto> UpdateVitals(int id, VisitVitalsRequest request)
    {
        var visit = db.Visits.FirstOrDefault(v => v.Id == id);
        if (visit is null) return NotFound<VisitDetailDto>("Không tìm thấy lượt khám");

        visit.VitalSignsJson = JsonSerializer.Serialize(request);
        visit.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();

        return Result<VisitDetailDto>.Ok(ToVisitDetail(visit), "Cập nhật sinh hiệu thành công");
    }

    public Result<VisitDetailDto> CompleteVisit(int id)
    {
        var visit = db.Visits.FirstOrDefault(v => v.Id == id);
        if (visit is null) return NotFound<VisitDetailDto>("Không tìm thấy lượt khám");

        var record = db.MedicalRecords.FirstOrDefault(r => r.VisitId == id);
        if (record is null) return Conflict<VisitDetailDto>("Không hoàn tất lượt khám nếu chưa có bệnh án");
        if (record.Status != MedicalStatuses.Completed) return Conflict<VisitDetailDto>("Bệnh án chưa hoàn tất");

        visit.Status = MedicalStatuses.Completed;
        visit.CompletedAt = DateTime.UtcNow;
        visit.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();

        return Result<VisitDetailDto>.Ok(ToVisitDetail(visit), "Hoàn tất lượt khám thành công");
    }

    public Result<VisitDetailDto> CancelVisit(int id, VisitCancelRequest request)
    {
        var visit = db.Visits.FirstOrDefault(v => v.Id == id);
        if (visit is null) return NotFound<VisitDetailDto>("Không tìm thấy lượt khám");

        visit.Status = MedicalStatuses.Cancelled;
        visit.CancelReason = request.CancelReason;
        visit.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();

        return Result<VisitDetailDto>.Ok(ToVisitDetail(visit), "Hủy lượt khám thành công");
    }

    public Result<MedicalRecordDetailDto> CreateMedicalRecord(MedicalRecordCreateRequest request)
    {
        var visit = db.Visits.FirstOrDefault(v => v.Id == request.VisitId);
        if (visit is null) return NotFound<MedicalRecordDetailDto>("Không tìm thấy lượt khám");
        
        if (visit.Status == MedicalStatuses.WaitingForExam)
        {
            visit.Status = MedicalStatuses.InProgress;
            visit.StartedAt = DateTime.UtcNow;
            db.SaveChanges();
        }

        if (visit.Status != MedicalStatuses.InProgress) return Conflict<MedicalRecordDetailDto>("Lượt khám chưa ở trạng thái đang khám");
        if (db.MedicalRecords.Any(r => r.VisitId == request.VisitId)) return Conflict<MedicalRecordDetailDto>("Lượt khám đã có bệnh án");
        if (string.IsNullOrWhiteSpace(request.DiagnosisText))
            return Invalid<MedicalRecordDetailDto>("Chẩn đoán không được để trống", "diagnosisText", "REQUIRED", "Chẩn đoán không được để trống");

        var record = new MedicalRecord
        {
            VisitId = visit.Id,
            PatientId = visit.PatientId,
            DoctorId = visit.DoctorId,
            DiagnosisCode = request.DiagnosisCode,
            DiagnosisText = request.DiagnosisText.Trim(),
            DoctorNote = request.DoctorNote,
            TreatmentPlan = request.TreatmentPlan,
            FollowUpDate = request.FollowUpDate
        };

        db.MedicalRecords.Add(record);
        db.SaveChanges();
        record.MedicalRecordCode = $"BA{record.Id:D3}";
        db.SaveChanges();

        return Result<MedicalRecordDetailDto>.Ok(ToMedicalRecordDetail(record), "Tạo bệnh án thành công", StatusCodes.Status201Created);
    }

    public Result<MedicalRecordDetailDto> GetMedicalRecord(int id)
    {
        var record = db.MedicalRecords.AsNoTracking().FirstOrDefault(r => r.Id == id);
        return record is null
            ? NotFound<MedicalRecordDetailDto>("Không tìm thấy bệnh án")
            : Result<MedicalRecordDetailDto>.Ok(ToMedicalRecordDetail(record), "Lấy thông tin bệnh án thành công");
    }

    public Result<MedicalRecordDetailDto> GetMedicalRecordByVisit(int visitId)
    {
        var record = db.MedicalRecords.AsNoTracking().FirstOrDefault(r => r.VisitId == visitId);
        return record is null
            ? NotFound<MedicalRecordDetailDto>("Không tìm thấy bệnh án")
            : Result<MedicalRecordDetailDto>.Ok(ToMedicalRecordDetail(record), "Lấy bệnh án theo lượt khám thành công");
    }

    public Result<MedicalRecordDetailDto> UpdateMedicalRecord(int id, MedicalRecordUpdateRequest request)
    {
        var record = db.MedicalRecords.FirstOrDefault(r => r.Id == id);
        if (record is null) return NotFound<MedicalRecordDetailDto>("Không tìm thấy bệnh án");
        if (record.Status != MedicalStatuses.Draft) return Conflict<MedicalRecordDetailDto>("Chỉ được sửa bệnh án ở trạng thái bản nháp");
        if (string.IsNullOrWhiteSpace(request.DiagnosisText))
            return Invalid<MedicalRecordDetailDto>("Chẩn đoán không được để trống", "diagnosisText", "REQUIRED", "Chẩn đoán không được để trống");

        record.DiagnosisCode = request.DiagnosisCode;
        record.DiagnosisText = request.DiagnosisText.Trim();
        record.DoctorNote = request.DoctorNote;
        record.TreatmentPlan = request.TreatmentPlan;
        record.FollowUpDate = request.FollowUpDate;
        record.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();

        return Result<MedicalRecordDetailDto>.Ok(ToMedicalRecordDetail(record), "Cập nhật bệnh án thành công");
    }

    public Result<MedicalRecordDetailDto> CompleteMedicalRecord(int id)
    {
        var record = db.MedicalRecords.FirstOrDefault(r => r.Id == id);
        if (record is null) return NotFound<MedicalRecordDetailDto>("Không tìm thấy bệnh án");
        if (string.IsNullOrWhiteSpace(record.DiagnosisText))
            return Invalid<MedicalRecordDetailDto>("Chẩn đoán không được để trống", "diagnosisText", "REQUIRED", "Chẩn đoán không được để trống");

        record.Status = MedicalStatuses.Completed;
        record.CompletedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();

        return Result<MedicalRecordDetailDto>.Ok(ToMedicalRecordDetail(record), "Hoàn tất bệnh án thành công");
    }

    public Result<PrescriptionDetailDto> CreatePrescription(PrescriptionCreateRequest request)
    {
        var record = db.MedicalRecords.FirstOrDefault(r => r.Id == request.MedicalRecordId);
        if (record is null) return NotFound<PrescriptionDetailDto>("Không tìm thấy bệnh án");

        var prescription = CreatePrescriptionEntity(record, request.Note);
        return Result<PrescriptionDetailDto>.Ok(ToPrescriptionDetail(prescription), "Tạo đơn thuốc thành công", StatusCodes.Status201Created);
    }

    public Result<PrescriptionDetailDto> GetPrescription(int id)
    {
        var prescription = db.Prescriptions.AsNoTracking().FirstOrDefault(p => p.Id == id);
        return prescription is null
            ? NotFound<PrescriptionDetailDto>("Không tìm thấy đơn thuốc")
            : Result<PrescriptionDetailDto>.Ok(ToPrescriptionDetail(prescription), "Lấy thông tin đơn thuốc thành công");
    }

    public Result<PrescriptionDetailDto> AddPrescriptionItem(int id, PrescriptionItemRequest request)
    {
        var prescription = db.Prescriptions.FirstOrDefault(p => p.Id == id);
        if (prescription is null) return NotFound<PrescriptionDetailDto>("Không tìm thấy đơn thuốc");
        var validation = ValidatePrescriptionItem<PrescriptionDetailDto>(request);
        if (validation is not null) return validation;

        CreatePrescriptionItemEntity(id, request);

        return Result<PrescriptionDetailDto>.Ok(ToPrescriptionDetail(prescription), "Thêm thuốc vào đơn thành công");
    }

    public Result<PrescriptionDetailDto> UpdatePrescriptionItem(int id, int itemId, PrescriptionItemRequest request)
    {
        var prescription = db.Prescriptions.FirstOrDefault(p => p.Id == id);
        if (prescription is null) return NotFound<PrescriptionDetailDto>("Không tìm thấy đơn thuốc");
        var item = db.PrescriptionItems.FirstOrDefault(i => i.Id == itemId && i.PrescriptionId == id);
        if (item is null) return NotFound<PrescriptionDetailDto>("Không tìm thấy thuốc trong đơn");
        var validation = ValidatePrescriptionItem<PrescriptionDetailDto>(request);
        if (validation is not null) return validation;

        item.MedicineId = request.MedicineId;
        item.MedicineNameSnapshot = request.MedicineNameSnapshot;
        item.UnitSnapshot = request.UnitSnapshot;
        item.Dosage = request.Dosage;
        item.Frequency = request.Frequency;
        item.DurationDays = request.DurationDays;
        item.Quantity = request.Quantity;
        item.UsageInstruction = request.UsageInstruction;
        item.Note = request.Note;
        db.SaveChanges();

        return Result<PrescriptionDetailDto>.Ok(ToPrescriptionDetail(prescription), "Cập nhật thuốc trong đơn thành công");
    }

    public Result<PrescriptionDetailDto> DeletePrescriptionItem(int id, int itemId)
    {
        var prescription = db.Prescriptions.FirstOrDefault(p => p.Id == id);
        if (prescription is null) return NotFound<PrescriptionDetailDto>("Không tìm thấy đơn thuốc");
        var item = db.PrescriptionItems.FirstOrDefault(i => i.Id == itemId && i.PrescriptionId == id);
        if (item is null) return NotFound<PrescriptionDetailDto>("Không tìm thấy thuốc trong đơn");

        db.PrescriptionItems.Remove(item);
        db.SaveChanges();

        return Result<PrescriptionDetailDto>.Ok(ToPrescriptionDetail(prescription), "Xóa thuốc khỏi đơn thành công");
    }

    public Result<PrescriptionSubmitDto> SubmitPrescription(int id, PrescriptionSubmitRequest? request)
    {
        using var transaction = db.Database.BeginTransaction();

        var prescription = db.Prescriptions.FirstOrDefault(p => p.Id == id);
        if (prescription is null && request?.MedicalRecordId is not null)
        {
            var recordToCreate = db.MedicalRecords.FirstOrDefault(r => r.Id == request.MedicalRecordId.Value);
            if (recordToCreate is null) return NotFound<PrescriptionSubmitDto>("Không tìm thấy bệnh án");
            prescription = CreatePrescriptionEntity(recordToCreate, request.Note);
        }

        if (prescription is null) return NotFound<PrescriptionSubmitDto>("Không tìm thấy đơn thuốc");

        if (request is not null)
        {
            prescription.Note = request.Note ?? prescription.Note;
            foreach (var itemRequest in request.Items)
            {
                var validation = ValidatePrescriptionItem<PrescriptionSubmitDto>(itemRequest);
                if (validation is not null) return validation;
            }

            db.PrescriptionItems.RemoveRange(db.PrescriptionItems.Where(i => i.PrescriptionId == prescription.Id));
            db.SaveChanges();
            foreach (var itemRequest in request.Items)
            {
                CreatePrescriptionItemEntity(prescription.Id, itemRequest);
            }
        }

        var items = db.PrescriptionItems.Where(i => i.PrescriptionId == prescription.Id).ToList();
        if (items.Count == 0)
            return Invalid<PrescriptionSubmitDto>("Đơn thuốc phải có ít nhất một loại thuốc", "items", "REQUIRED", "Đơn thuốc phải có ít nhất một loại thuốc");

        prescription.Status = MedicalStatuses.SentToPharmacy;
        prescription.SentToPharmacyAt = DateTime.UtcNow;
        db.SaveChanges();
        var outbox = CreatePrescriptionCreatedOutbox(prescription, items);
        db.SaveChanges();
        transaction.Commit();
        // Removed synchronous call to DispatchPrescriptionCreatedEvent, handled by Background Worker.

        var record = db.MedicalRecords.AsNoTracking().First(r => r.Id == prescription.MedicalRecordId);
        return Result<PrescriptionSubmitDto>.Ok(
            new(prescription.Id, prescription.PrescriptionCode, prescription.MedicalRecordId, record.MedicalRecordCode, prescription.Status, outbox.EventCode),
            "Chốt đơn thuốc thành công và đã tạo event gửi nhà thuốc");
    }

    public Result<PrescriptionDetailDto> CancelPrescription(int id, PrescriptionCancelRequest request)
    {
        var prescription = db.Prescriptions.FirstOrDefault(p => p.Id == id);
        if (prescription is null) return NotFound<PrescriptionDetailDto>("Không tìm thấy đơn thuốc");

        prescription.Status = MedicalStatuses.Cancelled;
        prescription.CancelledAt = DateTime.UtcNow;
        prescription.CancelReason = request.CancelReason;
        db.SaveChanges();

        return Result<PrescriptionDetailDto>.Ok(ToPrescriptionDetail(prescription), "Hủy đơn thuốc thành công");
    }

    public Result<IReadOnlyList<MedicineCatalogDto>> GetMedicineCatalog(string? name, string? activeIngredient, string? status)
    {
        try
        {
            var pharmacyBaseUrl = configuration["ServiceUrls:PharmacyBillingService"] ?? "http://pharmacy-billing-service:8080";
            var query = new List<string>
            {
                "page=1",
                "pageSize=200"
            };

            if (!string.IsNullOrWhiteSpace(name)) query.Add($"name={Uri.EscapeDataString(name)}");
            if (!string.IsNullOrWhiteSpace(activeIngredient)) query.Add($"activeIngredient={Uri.EscapeDataString(activeIngredient)}");
            if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}");

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{pharmacyBaseUrl.TrimEnd('/')}/api/medicines?{string.Join("&", query)}");
            CopyAuthorizationHeader(request);

            using var response = httpClientFactory.CreateClient().Send(request);
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                return Result<IReadOnlyList<MedicineCatalogDto>>.Fail(
                    "Không lấy được danh mục thuốc từ Pharmacy & Billing Service",
                    (int)response.StatusCode,
                    new ApiError("pharmacy", "UPSTREAM_ERROR", body));
            }

            var medicines = JsonSerializer.Deserialize<IReadOnlyList<MedicineCatalogDto>>(body, _jsonOptions) ?? [];
            return Result<IReadOnlyList<MedicineCatalogDto>>.Ok(medicines, "Lấy danh mục thuốc thành công");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Không lấy được danh mục thuốc từ Pharmacy & Billing Service.");
            return Result<IReadOnlyList<MedicineCatalogDto>>.Fail(
                "Không lấy được danh mục thuốc từ Pharmacy & Billing Service",
                StatusCodes.Status502BadGateway,
                new ApiError("pharmacy", "UPSTREAM_ERROR", ex.Message));
        }
    }

    public Result<ClinicalOrderDto> CreateClinicalOrder(ClinicalOrderCreateRequest request)
    {
        var record = db.MedicalRecords.FirstOrDefault(r => r.Id == request.MedicalRecordId);
        if (record is null) return NotFound<ClinicalOrderDto>("Không tìm thấy bệnh án");
        if (string.IsNullOrWhiteSpace(request.OrderType) || string.IsNullOrWhiteSpace(request.OrderName))
            return Invalid<ClinicalOrderDto>("Dữ liệu không hợp lệ", "orderName", "REQUIRED", "Loại chỉ định và tên chỉ định không được để trống");

        var order = new ClinicalOrder
        {
            MedicalRecordId = record.Id,
            PatientId = record.PatientId,
            DoctorId = record.DoctorId,
            OrderType = request.OrderType.Trim(),
            OrderName = request.OrderName.Trim(),
            Reason = request.Reason
        };

        db.ClinicalOrders.Add(order);
        db.SaveChanges();
        order.ClinicalOrderCode = $"CD{order.Id:D3}";
        db.SaveChanges();

        return Result<ClinicalOrderDto>.Ok(ToClinicalOrderDto(order), "Tạo chỉ định lâm sàng thành công", StatusCodes.Status201Created);
    }

    public Result<IReadOnlyList<ClinicalOrderDto>> GetClinicalOrders(int? medicalRecordId, int? patientId)
    {
        var orders = db.ClinicalOrders
            .AsNoTracking()
            .Where(o => medicalRecordId == null || o.MedicalRecordId == medicalRecordId)
            .Where(o => patientId == null || o.PatientId == patientId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => ToClinicalOrderDto(o))
            .ToList();

        return Result<IReadOnlyList<ClinicalOrderDto>>.Ok(orders, "Lấy danh sách chỉ định lâm sàng thành công");
    }

    public Result<EventResultDto> HandleAppointmentConfirmed(AppointmentConfirmedEventRequest request)
    {
        if (AlreadyProcessed(request.Source, request.EventCode))
        {
            return Result<EventResultDto>.Ok(new(request.EventCode, request.EventType, MedicalStatuses.Processed, "Event đã được xử lý trước đó"), "Event đã được xử lý trước đó");
        }

        using var transaction = db.Database.BeginTransaction();
        var patient = db.Patients.FirstOrDefault(p =>
            (!string.IsNullOrWhiteSpace(request.Data.PhoneNumber) && p.PhoneNumber == request.Data.PhoneNumber)
            || (!string.IsNullOrWhiteSpace(request.Data.CitizenId) && p.CitizenId == request.Data.CitizenId));

        if (patient is null)
        {
            patient = new Patient
            {
                FullName = request.Data.PatientName,
                DateOfBirth = request.Data.DateOfBirth,
                Gender = request.Data.Gender,
                PhoneNumber = request.Data.PhoneNumber,
                CitizenId = request.Data.CitizenId
            };
            db.Patients.Add(patient);
            db.SaveChanges();
            patient.PatientCode = $"BN{patient.Id:D3}";
        }

        var snapshot = db.AppointmentSnapshots.FirstOrDefault(a => a.AppointmentId == request.Data.AppointmentId);
        if (snapshot is null)
        {
            snapshot = new AppointmentSnapshot { AppointmentId = request.Data.AppointmentId };
            db.AppointmentSnapshots.Add(snapshot);
        }

        snapshot.PatientId = patient.Id;
        snapshot.PatientNameSnapshot = request.Data.PatientName;
        snapshot.DoctorId = request.Data.DoctorId;
        snapshot.DoctorNameSnapshot = request.Data.DoctorName;
        snapshot.SpecialtyId = request.Data.SpecialtyId;
        snapshot.SpecialtyNameSnapshot = request.Data.SpecialtyName;
        snapshot.ScheduledAt = request.Data.ScheduledAt;
        snapshot.QueueNumber = request.Data.QueueNumber;
        snapshot.Status = request.Data.Status;
        snapshot.ConfirmedAt = request.OccurredAt;
        AddInbox(request.Source, request.EventCode, request.EventType, JsonSerializer.Serialize(request));

        db.SaveChanges();
        transaction.Commit();

        return Result<EventResultDto>.Ok(new(request.EventCode, request.EventType, MedicalStatuses.Processed, "Đã lưu bệnh nhân và snapshot lịch hẹn"), "Xử lý event lịch hẹn thành công");
    }

    public Result<EventResultDto> HandlePatientCheckedIn(PatientCheckedInEventRequest request)
    {
        if (AlreadyProcessed(request.Source, request.EventCode))
        {
            return Result<EventResultDto>.Ok(new(request.EventCode, request.EventType, MedicalStatuses.Processed, "Event đã được xử lý trước đó"), "Event đã được xử lý trước đó");
        }

        var snapshot = db.AppointmentSnapshots.FirstOrDefault(a => a.AppointmentId == request.Data.AppointmentId);
        if (snapshot?.PatientId is null) return Conflict<EventResultDto>("Lịch hẹn chưa sẵn sàng để khám");
        var existingVisit = db.Visits.FirstOrDefault(v => v.AppointmentId == request.Data.AppointmentId);
        if (existingVisit is not null)
        {
            if (IsInProgressEvent(request.Data.Status) && existingVisit.Status == MedicalStatuses.WaitingForExam)
            {
                existingVisit.DoctorId = request.Data.DoctorId;
                existingVisit.Status = MedicalStatuses.InProgress;
                existingVisit.StartedAt = request.Data.CheckedInAt;
                existingVisit.UpdatedAt = DateTime.UtcNow;
            }

            AddInbox(request.Source, request.EventCode, request.EventType, JsonSerializer.Serialize(request));
            db.SaveChanges();
            return Result<EventResultDto>.Ok(new(request.EventCode, request.EventType, MedicalStatuses.Processed, $"Lượt khám {existingVisit.VisitCode} đã tồn tại"), "Event check-in đã được đồng bộ trước đó");
        }

        var isInProgress = IsInProgressEvent(request.Data.Status);
        var visit = new Visit
        {
            AppointmentId = snapshot.AppointmentId,
            PatientId = snapshot.PatientId.Value,
            DoctorId = request.Data.DoctorId,
            VisitDate = request.Data.CheckedInAt,
            Status = isInProgress ? MedicalStatuses.InProgress : MedicalStatuses.WaitingForExam,
            StartedAt = isInProgress ? request.Data.CheckedInAt : null
        };

        db.Visits.Add(visit);
        AddInbox(request.Source, request.EventCode, request.EventType, JsonSerializer.Serialize(request));
        db.SaveChanges();
        visit.VisitCode = $"LK{visit.Id:D3}";
        db.SaveChanges();

        return Result<EventResultDto>.Ok(new(request.EventCode, request.EventType, MedicalStatuses.Processed, $"Đã tạo lượt khám {visit.VisitCode}"), "Xử lý event check-in thành công");
    }

    private static bool IsInProgressEvent(string? status)
        => string.Equals(status, "InProgress", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, MedicalStatuses.InProgress, StringComparison.OrdinalIgnoreCase);

    public Result<IReadOnlyList<OutboxEventDto>> GetOutboxEvents(string? status)
    {
        var events = db.OutboxEvents
            .AsNoTracking()
            .Where(e => string.IsNullOrWhiteSpace(status) || e.Status == status)
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => ToOutboxDto(e))
            .ToList();

        return Result<IReadOnlyList<OutboxEventDto>>.Ok(events, "Lấy danh sách outbox event thành công");
    }

    public Result<OutboxEventDto> MarkOutboxPublished(int id)
    {
        var outbox = db.OutboxEvents.FirstOrDefault(e => e.Id == id);
        if (outbox is null) return NotFound<OutboxEventDto>("Không tìm thấy outbox event");

        outbox.Status = MedicalStatuses.Published;
        outbox.PublishedAt = DateTime.UtcNow;
        db.SaveChanges();

        return Result<OutboxEventDto>.Ok(ToOutboxDto(outbox), "Đánh dấu event đã gửi thành công");
    }

    private Prescription CreatePrescriptionEntity(MedicalRecord record, string? note)
    {
        var prescription = new Prescription
        {
            MedicalRecordId = record.Id,
            PatientId = record.PatientId,
            DoctorId = record.DoctorId,
            Note = note
        };
        db.Prescriptions.Add(prescription);
        db.SaveChanges();
        prescription.PrescriptionCode = $"DT{prescription.Id:D3}";
        db.SaveChanges();
        return prescription;
    }

    private PrescriptionItem CreatePrescriptionItemEntity(int prescriptionId, PrescriptionItemRequest request)
    {
        var item = new PrescriptionItem
        {
            PrescriptionId = prescriptionId,
            MedicineId = request.MedicineId,
            MedicineNameSnapshot = request.MedicineNameSnapshot,
            UnitSnapshot = request.UnitSnapshot,
            Dosage = request.Dosage,
            Frequency = request.Frequency,
            DurationDays = request.DurationDays,
            Quantity = request.Quantity,
            UsageInstruction = request.UsageInstruction,
            Note = request.Note
        };
        db.PrescriptionItems.Add(item);
        db.SaveChanges();
        item.PrescriptionItemCode = $"CTDT{item.Id:D3}";
        db.SaveChanges();
        return item;
    }

    private OutboxEvent CreatePrescriptionCreatedOutbox(Prescription prescription, IReadOnlyList<PrescriptionItem> items)
    {
        var record = db.MedicalRecords.AsNoTracking().First(r => r.Id == prescription.MedicalRecordId);
        var visit = db.Visits.AsNoTracking().First(v => v.Id == record.VisitId);
        var patient = db.Patients.AsNoTracking().First(p => p.Id == prescription.PatientId);
        var snapshot = visit.AppointmentId is null
            ? null
            : db.AppointmentSnapshots.AsNoTracking().FirstOrDefault(a => a.AppointmentId == visit.AppointmentId);

        var outbox = new OutboxEvent
        {
            EventType = "prescription.created",
            AggregateType = nameof(Prescription),
            AggregateId = prescription.Id,
            Payload = string.Empty
        };
        db.OutboxEvents.Add(outbox);
        db.SaveChanges();

        var eventCode = $"N2EV{outbox.Id:D3}";
        outbox.EventCode = eventCode;

        var payload = new
        {
            eventCode = eventCode,
            eventType = "prescription.created",
            source = "MedicalRecordService",
            occurredAt = DateTime.UtcNow,
            prescriptionId = prescription.Id,
            prescriptionCode = prescription.PrescriptionCode,
            medicalRecordId = record.Id,
            visitId = visit.Id,
            appointmentId = visit.AppointmentId,
            patientId = patient.Id,
            patientCode = patient.PatientCode,
            patientName = patient.FullName,
            phoneNumber = patient.PhoneNumber,
            doctorId = prescription.DoctorId,
            doctorName = snapshot?.DoctorNameSnapshot ?? "Unknown Doctor",
            diagnosis = record.DiagnosisText,
            items = items.Select(i => new
            {
                medicineId = i.MedicineId,
                medicineName = i.MedicineNameSnapshot,
                unit = i.UnitSnapshot,
                dosage = i.Dosage,
                frequency = i.Frequency,
                durationDays = i.DurationDays,
                quantity = (int)Math.Ceiling(i.Quantity),
                usageInstruction = i.UsageInstruction
            }).ToList()
        };

        outbox.Payload = JsonSerializer.Serialize(payload);
        db.SaveChanges();
        return outbox;
    }

    private void DispatchPrescriptionCreatedEvent(Prescription prescription, IReadOnlyList<PrescriptionItem> items, OutboxEvent outbox)
    {
        // No-op. Handled by background worker.
    }

    private void CopyAuthorizationHeader(HttpRequestMessage request)
    {
        var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization) && AuthenticationHeaderValue.TryParse(authorization, out var header))
        {
            request.Headers.Authorization = header;
        }
    }

    private Result<T>? ValidatePrescriptionItem<T>(PrescriptionItemRequest request)
    {
        if (request.Quantity <= 0) return Invalid<T>("Số lượng phải lớn hơn 0", "quantity", "GREATER_THAN_ZERO", "Số lượng phải lớn hơn 0");
        if (request.DurationDays <= 0) return Invalid<T>("Số ngày dùng phải lớn hơn 0", "durationDays", "GREATER_THAN_ZERO", "Số ngày dùng phải lớn hơn 0");
        if (string.IsNullOrWhiteSpace(request.MedicineNameSnapshot)) return Invalid<T>("Tên thuốc không được để trống", "medicineNameSnapshot", "REQUIRED", "Tên thuốc không được để trống");
        if (string.IsNullOrWhiteSpace(request.Dosage)) return Invalid<T>("Liều dùng không được để trống", "dosage", "REQUIRED", "Liều dùng không được để trống");
        if (string.IsNullOrWhiteSpace(request.Frequency)) return Invalid<T>("Tần suất dùng không được để trống", "frequency", "REQUIRED", "Tần suất dùng không được để trống");
        return null;
    }

    private void AddInbox(string source, string eventCode, string eventType, string payload)
    {
        db.InboxEvents.Add(new InboxEvent
        {
            EventCode = eventCode,
            SourceService = source,
            EventType = eventType,
            Payload = payload
        });
    }

    private bool AlreadyProcessed(string source, string eventCode)
        => db.InboxEvents.AsNoTracking().Any(e => e.SourceService == source && e.EventCode == eventCode);

    private Patient? FindPatient(int id) => db.Patients.FirstOrDefault(p => p.Id == id && !p.IsDeleted);

    private static PatientSummaryDto ToSummary(Patient patient)
        => new(patient.Id, patient.PatientCode, patient.FullName, patient.PhoneNumber, patient.Status);

    private static PatientDetailDto ToDetail(Patient patient)
        => new(patient.Id, patient.PatientCode, patient.FullName, patient.DateOfBirth, patient.Gender, patient.PhoneNumber,
            patient.Email, patient.Address, patient.CitizenId, patient.BloodType, patient.AllergyNote, patient.MedicalHistory,
            patient.Status, patient.CreatedAt, patient.UpdatedAt);

    private VisitDetailDto ToVisitDetail(Visit visit)
    {
        var patient = db.Patients.AsNoTracking().FirstOrDefault(p => p.Id == visit.PatientId);
        var snapshot = visit.AppointmentId is null
            ? null
            : db.AppointmentSnapshots.AsNoTracking().FirstOrDefault(a => a.AppointmentId == visit.AppointmentId);

        return new(visit.Id, visit.VisitCode, visit.AppointmentId, visit.PatientId, patient?.PatientCode, patient?.FullName ?? string.Empty,
            visit.DoctorId, snapshot?.DoctorNameSnapshot, visit.VisitDate, visit.ChiefComplaint, visit.Symptoms, visit.VitalSignsJson,
            visit.Status, visit.StartedAt, visit.CompletedAt, visit.CancelReason);
    }

    private MedicalRecordDetailDto ToMedicalRecordDetail(MedicalRecord record)
    {
        var patient = db.Patients.AsNoTracking().FirstOrDefault(p => p.Id == record.PatientId);
        return new(record.Id, record.MedicalRecordCode, record.VisitId, record.PatientId, patient?.PatientCode, record.DoctorId,
            record.DiagnosisCode, record.DiagnosisText, record.DoctorNote, record.TreatmentPlan, record.FollowUpDate,
            record.Status, record.CreatedAt, record.CompletedAt);
    }

    private PrescriptionDetailDto ToPrescriptionDetail(Prescription prescription)
    {
        var record = db.MedicalRecords.AsNoTracking().FirstOrDefault(r => r.Id == prescription.MedicalRecordId);
        var patient = db.Patients.AsNoTracking().FirstOrDefault(p => p.Id == prescription.PatientId);
        var items = db.PrescriptionItems.AsNoTracking()
            .Where(i => i.PrescriptionId == prescription.Id)
            .Select(i => new PrescriptionItemDto(i.Id, i.PrescriptionItemCode, i.MedicineId, i.MedicineNameSnapshot, i.UnitSnapshot,
                i.Dosage, i.Frequency, i.DurationDays, i.Quantity, i.UsageInstruction, i.Note))
            .ToList();

        return new(prescription.Id, prescription.PrescriptionCode, prescription.MedicalRecordId, record?.MedicalRecordCode,
            prescription.PatientId, patient?.PatientCode, prescription.DoctorId, prescription.Status, prescription.Note,
            prescription.CreatedAt, prescription.SentToPharmacyAt, items);
    }

    private static ClinicalOrderDto ToClinicalOrderDto(ClinicalOrder order)
        => new(order.Id, order.ClinicalOrderCode, order.MedicalRecordId, order.PatientId, order.DoctorId,
            order.OrderType, order.OrderName, order.Reason, order.Status, order.CreatedAt);

    private static OutboxEventDto ToOutboxDto(OutboxEvent e)
        => new(e.Id, e.EventCode, e.EventType, e.AggregateType, e.AggregateId, e.Payload, e.Status, e.OccurredAt, e.PublishedAt, e.RetryCount);

    private static Result<T> NotFound<T>(string message)
        => Result<T>.Fail(message, StatusCodes.Status404NotFound, new ApiError("id", "NOT_FOUND", message));

    private static Result<T> Conflict<T>(string message)
        => Result<T>.Fail(message, StatusCodes.Status409Conflict, new ApiError("state", "CONFLICT", message));

    private static Result<T> Invalid<T>(string message, string field, string code, string errorMessage)
        => Result<T>.Fail(message, StatusCodes.Status400BadRequest, new ApiError(field, code, errorMessage));
}
