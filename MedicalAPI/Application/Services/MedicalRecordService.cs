using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MedicalAPI.Application.Common;
using MedicalAPI.Application.DTOs;
using MedicalAPI.Domain.Constants;
using MedicalAPI.Domain.Entities;
using MedicalAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MedicalAPI.Application.Services;

public sealed class MedicalRecordService(
    MedicalDbContext db,
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    ILogger<MedicalRecordService> logger) : IMedicalRecordService
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public Result<PagedList<PatientDetailDto>> SearchPatients(string? keyword, int pageNumber, int pageSize)
    {
        var normalized = keyword?.Trim();
        var query = db.Patients
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Where(p => string.IsNullOrWhiteSpace(normalized)
                || p.FullName.Contains(normalized)
                || (p.PatientCode != null && p.PatientCode.Contains(normalized))
                || (p.PhoneNumber != null && p.PhoneNumber.Contains(normalized)));

        if (IsCurrentPatient())
        {
            var currentPatientId = CurrentPatientIdFromClaims();
            if (currentPatientId is null) return Forbidden<PagedList<PatientDetailDto>>("Token bệnh nhân thiếu PatientId");
            query = query.Where(p => p.Id == currentPatientId.Value);
        }
        else if (IsCurrentDoctor())
        {
            var currentDoctorId = CurrentDoctorId();
            if (currentDoctorId is null) return Forbidden<PagedList<PatientDetailDto>>("Token bác sĩ thiếu DoctorId");
            query = query.Where(p =>
                db.Visits.Any(v => v.PatientId == p.Id && v.DoctorId == currentDoctorId.Value)
                || db.MedicalRecords.Any(r => r.PatientId == p.Id && r.DoctorId == currentDoctorId.Value));
        }

        var patients = query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => ToDetail(p))
            .ToList();

        return Result<PagedList<PatientDetailDto>>.Ok(
            PagedList<PatientDetailDto>.Create(patients, pageNumber, pageSize),
            "Lấy danh sách bệnh nhân thành công");
    }

    public Result<IReadOnlyList<PatientLookupDto>> LookupPatientsForBooking(string? keyword, int limit)
    {
        var normalized = keyword?.Trim();
        var safeLimit = Math.Clamp(limit <= 0 ? 20 : limit, 1, 50);
        var lookup = NormalizeLookupText(normalized);

        var patients = db.Patients
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new PatientLookupDto(p.Id, p.PatientCode, p.FullName, p.PhoneNumber, p.DateOfBirth, p.Gender, p.Status))
            .AsEnumerable()
            .Where(p => string.IsNullOrWhiteSpace(lookup)
                || NormalizeLookupText(p.FullName).Contains(lookup)
                || NormalizeLookupText(p.PatientCode).Contains(lookup)
                || NormalizeLookupText(p.PhoneNumber).Contains(lookup))
            .OrderBy(p => p.FullName)
            .ThenBy(p => p.Id)
            .Take(safeLimit)
            .ToList();

        return Result<IReadOnlyList<PatientLookupDto>>.Ok(patients, "Tìm bệnh nhân đặt hộ thành công");
    }

    public Result<PatientDetailDto> GetPatient(int id)
    {
        var patient = FindPatient(id);
        if (patient is null) return NotFound<PatientDetailDto>("Không tìm thấy bệnh nhân");
        var access = EnsurePatientReadAccess<PatientDetailDto>(patient.Id);
        return access ?? Result<PatientDetailDto>.Ok(ToDetail(patient), "Lấy thông tin bệnh nhân thành công");
    }

    public Result<PatientDetailDto> GetPatientByKey(string patientKey, int? currentUserId, int? currentPatientId, string? currentEmail, string? currentFullName)
    {
        if (IsCurrentPatient())
        {
            if (CurrentPatientIdFromClaims() is not int claimPatientId)
            {
                return Forbidden<PatientDetailDto>("Token bệnh nhân thiếu PatientId");
            }

            var resolvedForPatient = ResolvePatientId(patientKey, currentUserId, claimPatientId, null, null);
            if (resolvedForPatient != claimPatientId)
            {
                return GetPatient(claimPatientId);
            }

            return GetPatient(claimPatientId);
        }

        var patientId = ResolvePatientId(patientKey, currentUserId, currentPatientId, currentEmail, currentFullName);
        if (patientId is null)
        {
            return Invalid<PatientDetailDto>("Mã bệnh nhân không hợp lệ", "patientKey", "INVALID", "Mã bệnh nhân phải là số ID hoặc dạng BN025");
        }

        return GetPatient(patientId.Value);
    }

    public Result<PatientDetailDto> CreatePatient(PatientCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Invalid<PatientDetailDto>("Dữ liệu không hợp lệ", "fullName", "REQUIRED", "Họ tên không được để trống");

        var patient = new Patient
        {
            FullName = CapitalizeFullName(request.FullName.Trim()),
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

        return Result<PatientDetailDto>.Ok(ToDetail(patient), "Tạo hồ sơ bệnh nhân thành công", StatusCodes.Status201Created);
    }

    public Result<PatientDetailDto> UpdatePatient(int id, PatientUpdateRequest request)
    {
        var patient = FindPatient(id);
        if (patient is null) return NotFound<PatientDetailDto>("Không tìm thấy bệnh nhân");
        if (IsCurrentPatient())
        {
            var currentPatientId = CurrentPatientIdFromClaims();
            if (currentPatientId != patient.Id)
            {
                return Forbidden<PatientDetailDto>("Bệnh nhân chỉ được cập nhật hồ sơ của chính mình");
            }
        }
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Invalid<PatientDetailDto>("Dữ liệu không hợp lệ", "fullName", "REQUIRED", "Họ tên không được để trống");

        ApplyPatientUpdate(patient, request, allowStatusUpdate: !IsCurrentPatient());
        db.SaveChanges();

        return Result<PatientDetailDto>.Ok(ToDetail(patient), "Cập nhật hồ sơ bệnh nhân thành công");
    }

    public Result<bool> DeletePatient(int id)
    {
        var patient = FindPatient(id);
        if (patient is null) return NotFound<bool>("Không tìm thấy bệnh nhân");

        var hasClinicalData = db.Visits.Any(v => v.PatientId == id)
            || db.MedicalRecords.Any(r => r.PatientId == id)
            || db.Prescriptions.Any(p => p.PatientId == id)
            || db.ClinicalOrders.Any(o => o.PatientId == id);

        if (hasClinicalData)
        {
            patient.IsDeleted = true;
            patient.Status = "Đã xóa";
            patient.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.Patients.Remove(patient);
        }

        db.SaveChanges();

        return Result<bool>.Ok(true, "Xóa bệnh nhân thành công");
    }

    public Result<PatientDetailDto> UpdateCurrentPatient(PatientUpdateRequest request)
    {
        var patientId = CurrentPatientIdFromClaims();
        if (patientId is null) return Forbidden<PatientDetailDto>("Token bệnh nhân thiếu PatientId");

        var patient = FindPatient(patientId.Value);
        if (patient is null) return NotFound<PatientDetailDto>("Không tìm thấy bệnh nhân");
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Invalid<PatientDetailDto>("Dữ liệu không hợp lệ", "fullName", "REQUIRED", "Họ tên không được để trống");

        ApplyPatientUpdate(patient, request, allowStatusUpdate: false);
        db.SaveChanges();

        return Result<PatientDetailDto>.Ok(ToDetail(patient), "Cập nhật hồ sơ cá nhân thành công");
    }

    private static void ApplyPatientUpdate(Patient patient, PatientUpdateRequest request, bool allowStatusUpdate)
    {
        patient.FullName = CapitalizeFullName(request.FullName.Trim());
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = request.Gender;
        patient.PhoneNumber = request.PhoneNumber;
        patient.Email = request.Email;
        patient.Address = request.Address;
        patient.CitizenId = request.CitizenId;
        patient.BloodType = request.BloodType;
        patient.AllergyNote = request.AllergyNote;
        patient.MedicalHistory = request.MedicalHistory;
        if (allowStatusUpdate)
        {
            patient.Status = string.IsNullOrWhiteSpace(request.Status) ? patient.Status : request.Status;
        }
        patient.UpdatedAt = DateTime.UtcNow;
    }

    public Result<PatientHistoryDto> GetPatientHistory(int id)
    {
        var patient = FindPatient(id);
        if (patient is null) return NotFound<PatientHistoryDto>("Không tìm thấy bệnh nhân");
        var access = EnsurePatientReadAccess<PatientHistoryDto>(patient.Id);
        if (access is not null) return access;

        var visits = db.Visits.AsNoTracking().Where(v => v.PatientId == id).ToList().Select(ToVisitDetail).ToList();
        var records = db.MedicalRecords.AsNoTracking().Where(r => r.PatientId == id).ToList().Select(ToMedicalRecordDetail).ToList();
        var prescriptions = db.Prescriptions.AsNoTracking().Where(p => p.PatientId == id).ToList().Select(ToPrescriptionDetail).ToList();

        return Result<PatientHistoryDto>.Ok(new(ToDetail(patient), visits, records, prescriptions), "Lấy lịch sử khám thành công");
    }

    public Result<PatientHistoryDto> GetPatientHistoryByKey(string patientKey, int? currentUserId, int? currentPatientId, string? currentEmail, string? currentFullName)
    {
        if (IsCurrentPatient())
        {
            if (CurrentPatientIdFromClaims() is not int claimPatientId)
            {
                return Forbidden<PatientHistoryDto>("Token bệnh nhân thiếu PatientId");
            }

            var resolvedForPatient = ResolvePatientId(patientKey, currentUserId, claimPatientId, null, null);
            if (resolvedForPatient != claimPatientId)
            {
                return GetPatientHistory(claimPatientId);
            }

            return GetPatientHistory(claimPatientId);
        }

        var patientId = ResolvePatientId(patientKey, currentUserId, currentPatientId, currentEmail, currentFullName);
        if (patientId is null)
        {
            return Invalid<PatientHistoryDto>("Mã bệnh nhân không hợp lệ", "patientKey", "INVALID", "Mã bệnh nhân phải là số ID hoặc dạng BN025");
        }

        return GetPatientHistory(patientId.Value);
    }

    public Result<PatientDetailDto> GetCurrentPatient()
    {
        var patientId = CurrentPatientIdFromClaims();
        return patientId is null
            ? Forbidden<PatientDetailDto>("Token bệnh nhân thiếu PatientId")
            : GetPatient(patientId.Value);
    }

    public Result<PatientHistoryDto> GetCurrentPatientHistory()
    {
        var patientId = CurrentPatientIdFromClaims();
        return patientId is null
            ? Forbidden<PatientHistoryDto>("Token bệnh nhân thiếu PatientId")
            : GetPatientHistory(patientId.Value);
    }

    public Result<PatientClinicalTimelineDto> GetCurrentPatientClinicalTimeline()
    {
        var patientId = CurrentPatientIdFromClaims();
        return patientId is null
            ? Forbidden<PatientClinicalTimelineDto>("Token bệnh nhân thiếu PatientId")
            : GetPatientClinicalTimeline(patientId.Value);
    }

    public Result<PatientClinicalTimelineDto> GetPatientClinicalTimeline(int patientId)
    {
        var patient = FindPatient(patientId);
        if (patient is null) return NotFound<PatientClinicalTimelineDto>("Không tìm thấy bệnh nhân");
        var access = EnsurePatientReadAccess<PatientClinicalTimelineDto>(patient.Id);
        if (access is not null) return access;

        var bundles = db.Visits.AsNoTracking()
            .Where(v => v.PatientId == patient.Id)
            .OrderByDescending(v => v.VisitDate)
            .ToList()
            .Select(BuildVisitBundle)
            .ToList();

        return Result<PatientClinicalTimelineDto>.Ok(new(ToDetail(patient), bundles), "Lấy timeline lâm sàng thành công");
    }

    public Result<IReadOnlyList<VisitDetailDto>> GetTodayVisits(int? doctorId)
    {
        var today = DateTime.UtcNow.Date;
        if (IsCurrentDoctor())
        {
            var currentDoctorId = CurrentDoctorId();
            if (currentDoctorId is null) return Forbidden<IReadOnlyList<VisitDetailDto>>("Token bác sĩ thiếu DoctorId");
            doctorId = currentDoctorId.Value;
        }

        var query = db.Visits
            .AsNoTracking()
            .Where(v => v.VisitDate >= today && v.VisitDate < today.AddDays(1))
            .Where(v => doctorId == null || v.DoctorId == doctorId);

        if (IsCurrentPatient())
        {
            var currentPatientId = CurrentPatientIdFromClaims();
            if (currentPatientId is null) return Forbidden<IReadOnlyList<VisitDetailDto>>("Token bệnh nhân thiếu PatientId");
            query = query.Where(v => v.PatientId == currentPatientId.Value);
        }

        var visits = query
            .OrderBy(v => v.VisitDate)
            .ToList()
            .Select(ToVisitDetail)
            .ToList();

        return Result<IReadOnlyList<VisitDetailDto>>.Ok(visits, "Lấy danh sách lượt khám hôm nay thành công");
    }

    public Result<VisitDetailDto> GetVisit(int id)
    {
        var visit = db.Visits.AsNoTracking().FirstOrDefault(v => v.Id == id);
        if (visit is null) return NotFound<VisitDetailDto>("Không tìm thấy lượt khám");
        var access = EnsureVisitReadAccess<VisitDetailDto>(visit);
        return access ?? Result<VisitDetailDto>.Ok(ToVisitDetail(visit), "Lấy thông tin lượt khám thành công");
    }

    public Result<VisitDetailDto> GetVisitByAppointment(int appointmentId)
    {
        var visit = db.Visits.AsNoTracking().FirstOrDefault(v => v.AppointmentId == appointmentId);
        if (visit is null) return NotFound<VisitDetailDto>("Không tìm thấy lượt khám tương ứng với lịch hẹn");
        var access = EnsureVisitReadAccess<VisitDetailDto>(visit);
        return access ?? Result<VisitDetailDto>.Ok(ToVisitDetail(visit), "Lấy lượt khám theo lịch hẹn thành công");
    }

    public Result<VisitDetailDto> CreateVisit(VisitCreateRequest request)
    {
        if (FindPatient(request.PatientId) is null) return NotFound<VisitDetailDto>("Không tìm thấy bệnh nhân");
        if (request.AppointmentId.HasValue && db.Visits.Any(v => v.AppointmentId == request.AppointmentId))
            return Conflict<VisitDetailDto>("Lịch hẹn đã có lượt khám");
        if (IsCurrentDoctor() && !CurrentDoctorOwns(request.DoctorId))
            return Forbidden<VisitDetailDto>("Bác sĩ chỉ được tạo lượt khám thuộc mình");

        var visit = new Visit
        {
            AppointmentId = request.AppointmentId,
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            ChiefComplaint = request.ChiefComplaint,
            Symptoms = request.Symptoms
        };

        db.Visits.Add(visit);
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException exception) when (IsDuplicateVisitAppointment(exception))
        {
            db.ChangeTracker.Clear();
            return Conflict<VisitDetailDto>("Lịch hẹn đã có lượt khám");
        }
        visit.VisitCode = $"LK{visit.Id:D3}";
        db.SaveChanges();

        return Result<VisitDetailDto>.Ok(ToVisitDetail(visit), "Tạo lượt khám thành công", StatusCodes.Status201Created);
    }

    public Result<VisitDetailDto> StartVisit(int id, VisitStartRequest request)
    {
        var visit = db.Visits.FirstOrDefault(v => v.Id == id);
        if (visit is null) return NotFound<VisitDetailDto>("Không tìm thấy lượt khám");
        if (IsCurrentDoctor() && (!CurrentDoctorOwns(visit.DoctorId) || !CurrentDoctorOwns(request.DoctorId)))
            return Forbidden<VisitDetailDto>("Bác sĩ chỉ được bắt đầu lượt khám thuộc mình");
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
        if (IsCurrentPatient()) return Forbidden<VisitDetailDto>("Bệnh nhân không được cập nhật sinh hiệu");
        if (IsCurrentDoctor() && !CurrentDoctorOwns(visit.DoctorId))
            return Forbidden<VisitDetailDto>("Bác sĩ chỉ được cập nhật sinh hiệu lượt khám thuộc mình");
        if (visit.Status == MedicalStatuses.InProgress || visit.Status == MedicalStatuses.Completed || visit.Status == MedicalStatuses.Cancelled)
            return Conflict<VisitDetailDto>("Chỉ cập nhật sinh hiệu trước khi bác sĩ bắt đầu khám.");
        if (!HasMeasuredVitalSigns(request) && string.IsNullOrWhiteSpace(request.Note))
            return Invalid<VisitDetailDto>(
                "Dữ liệu sinh hiệu không hợp lệ",
                "vitalSigns",
                "REQUIRED",
                "Vui lòng nhập ít nhất một chỉ số sinh hiệu hoặc ghi chú điều dưỡng.");

        visit.VitalSignsJson = JsonSerializer.Serialize(request);
        visit.Status = MedicalStatuses.WaitingForExam;
        visit.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();

        return Result<VisitDetailDto>.Ok(ToVisitDetail(visit), "Cập nhật sinh hiệu thành công");
    }

    public Result<VisitDetailDto> CompleteVisit(int id)
    {
        var visit = db.Visits.FirstOrDefault(v => v.Id == id);
        if (visit is null) return NotFound<VisitDetailDto>("Không tìm thấy lượt khám");
        if (IsCurrentDoctor() && !CurrentDoctorOwns(visit.DoctorId))
            return Forbidden<VisitDetailDto>("Bác sĩ chỉ được hoàn tất lượt khám thuộc mình");

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
        if (IsCurrentPatient()) return Forbidden<VisitDetailDto>("Bệnh nhân không được hủy lượt khám từ N2");
        if (IsCurrentDoctor() && !CurrentDoctorOwns(visit.DoctorId))
            return Forbidden<VisitDetailDto>("Bác sĩ chỉ được hủy lượt khám thuộc mình");

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
        if (IsCurrentDoctor() && !CurrentDoctorOwns(visit.DoctorId))
            return Forbidden<MedicalRecordDetailDto>("Bác sĩ chỉ được tạo bệnh án cho lượt khám thuộc mình");
        
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
            DiagnosisSpecialty = request.DiagnosisSpecialty,
            DiagnosisText = request.DiagnosisText.Trim(),
            DoctorNote = request.DoctorNote,
            TreatmentPlan = request.TreatmentPlan,
            FollowUpDate = request.FollowUpDate
        };

        db.MedicalRecords.Add(record);
        db.SaveChanges();
        record.MedicalRecordCode = $"BA{record.Id:D3}";
        db.SaveChanges();
        CreateMedicalRecordOutbox(record, "medical_record.created");

        return Result<MedicalRecordDetailDto>.Ok(ToMedicalRecordDetail(record), "Tạo bệnh án thành công", StatusCodes.Status201Created);
    }

    public Result<MedicalRecordDetailDto> GetMedicalRecord(int id)
    {
        var record = db.MedicalRecords.AsNoTracking().FirstOrDefault(r => r.Id == id);
        if (record is null) return NotFound<MedicalRecordDetailDto>("Không tìm thấy bệnh án");
        var access = EnsureRecordReadAccess<MedicalRecordDetailDto>(record);
        return access ?? Result<MedicalRecordDetailDto>.Ok(ToMedicalRecordDetail(record), "Lấy thông tin bệnh án thành công");
    }

    public Result<MedicalRecordDetailDto> GetMedicalRecordByVisit(int visitId)
    {
        var record = db.MedicalRecords.AsNoTracking().FirstOrDefault(r => r.VisitId == visitId);
        if (record is null) return NotFound<MedicalRecordDetailDto>("Không tìm thấy bệnh án");
        var access = EnsureRecordReadAccess<MedicalRecordDetailDto>(record);
        return access ?? Result<MedicalRecordDetailDto>.Ok(ToMedicalRecordDetail(record), "Lấy bệnh án theo lượt khám thành công");
    }

    public Result<MedicalRecordDetailDto> UpdateMedicalRecord(int id, MedicalRecordUpdateRequest request)
    {
        var record = db.MedicalRecords.FirstOrDefault(r => r.Id == id);
        if (record is null) return NotFound<MedicalRecordDetailDto>("Không tìm thấy bệnh án");
        if (IsCurrentDoctor() && !CurrentDoctorOwns(record.DoctorId))
            return Forbidden<MedicalRecordDetailDto>("Bác sĩ chỉ được sửa bệnh án thuộc mình");
        if (record.Status != MedicalStatuses.Draft) return Conflict<MedicalRecordDetailDto>("Chỉ được sửa bệnh án ở trạng thái bản nháp");
        if (string.IsNullOrWhiteSpace(request.DiagnosisText))
            return Invalid<MedicalRecordDetailDto>("Chẩn đoán không được để trống", "diagnosisText", "REQUIRED", "Chẩn đoán không được để trống");

        record.DiagnosisCode = request.DiagnosisCode;
        record.DiagnosisSpecialty = request.DiagnosisSpecialty;
        record.DiagnosisText = request.DiagnosisText.Trim();
        record.DoctorNote = request.DoctorNote;
        record.TreatmentPlan = request.TreatmentPlan;
        record.FollowUpDate = request.FollowUpDate;
        record.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();
        CreateMedicalRecordOutbox(record, "medical_record.updated");

        return Result<MedicalRecordDetailDto>.Ok(ToMedicalRecordDetail(record), "Cập nhật bệnh án thành công");
    }

    public Result<MedicalRecordDetailDto> CompleteMedicalRecord(int id)
    {
        var record = db.MedicalRecords.FirstOrDefault(r => r.Id == id);
        if (record is null) return NotFound<MedicalRecordDetailDto>("Không tìm thấy bệnh án");
        if (IsCurrentDoctor() && !CurrentDoctorOwns(record.DoctorId))
            return Forbidden<MedicalRecordDetailDto>("Bác sĩ chỉ được hoàn tất bệnh án thuộc mình");
        if (string.IsNullOrWhiteSpace(record.DiagnosisText))
            return Invalid<MedicalRecordDetailDto>("Chẩn đoán không được để trống", "diagnosisText", "REQUIRED", "Chẩn đoán không được để trống");

        record.Status = MedicalStatuses.Completed;
        record.CompletedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();
        CreateMedicalRecordOutbox(record, "medical_record.updated");

        return Result<MedicalRecordDetailDto>.Ok(ToMedicalRecordDetail(record), "Hoàn tất bệnh án thành công");
    }

    public Result<CompleteMedicalRecordDto> GetCompleteMedicalRecord(int id)
    {
        var record = db.MedicalRecords.AsNoTracking().FirstOrDefault(r => r.Id == id);
        if (record is null) return NotFound<CompleteMedicalRecordDto>("Không tìm thấy bệnh án");
        var access = EnsureRecordReadAccess<CompleteMedicalRecordDto>(record);
        if (access is not null) return access;

        var visit = db.Visits.AsNoTracking().FirstOrDefault(v => v.Id == record.VisitId);
        if (visit is null) return NotFound<CompleteMedicalRecordDto>("Không tìm thấy lượt khám của bệnh án");
        var patient = FindPatient(record.PatientId);
        if (patient is null) return NotFound<CompleteMedicalRecordDto>("Không tìm thấy bệnh nhân");

        return Result<CompleteMedicalRecordDto>.Ok(BuildCompleteMedicalRecord(patient, visit, record), "Lấy bệnh án hoàn chỉnh thành công");
    }

    public Result<string> ExportMedicalRecordHtml(int id)
    {
        var complete = GetCompleteMedicalRecord(id);
        if (!complete.IsSuccess || complete.Data is null)
        {
            return Result<string>.Fail(complete.Message, complete.StatusCode, complete.Errors.ToArray());
        }

        return Result<string>.Ok(BuildRecordHtml(complete.Data), "Xuất hồ sơ bệnh án HTML thành công");
    }

    public Result<PrescriptionDetailDto> CreatePrescription(PrescriptionCreateRequest request)
    {
        var record = db.MedicalRecords.FirstOrDefault(r => r.Id == request.MedicalRecordId);
        if (record is null) return NotFound<PrescriptionDetailDto>("Không tìm thấy bệnh án");
        if (IsCurrentDoctor() && !CurrentDoctorOwns(record.DoctorId))
            return Forbidden<PrescriptionDetailDto>("Bác sĩ chỉ được kê đơn cho bệnh án thuộc mình");

        var prescription = CreatePrescriptionEntity(record, request.Note);
        return Result<PrescriptionDetailDto>.Ok(ToPrescriptionDetail(prescription), "Tạo đơn thuốc thành công", StatusCodes.Status201Created);
    }

    public Result<PrescriptionDetailDto> GetPrescription(int id)
    {
        var prescription = db.Prescriptions.AsNoTracking().FirstOrDefault(p => p.Id == id);
        if (prescription is null) return NotFound<PrescriptionDetailDto>("Không tìm thấy đơn thuốc");
        var access = EnsurePrescriptionReadAccess<PrescriptionDetailDto>(prescription);
        return access ?? Result<PrescriptionDetailDto>.Ok(ToPrescriptionDetail(prescription), "Lấy thông tin đơn thuốc thành công");
    }

    public Result<PrescriptionDetailDto> AddPrescriptionItem(int id, PrescriptionItemRequest request)
    {
        var prescription = db.Prescriptions.FirstOrDefault(p => p.Id == id);
        if (prescription is null) return NotFound<PrescriptionDetailDto>("Không tìm thấy đơn thuốc");
        if (IsCurrentDoctor() && !CurrentDoctorOwns(prescription.DoctorId))
            return Forbidden<PrescriptionDetailDto>("Bác sĩ chỉ được sửa đơn thuốc thuộc mình");
        var validation = ValidatePrescriptionItem<PrescriptionDetailDto>(request);
        if (validation is not null) return validation;

        CreatePrescriptionItemEntity(id, request);

        return Result<PrescriptionDetailDto>.Ok(ToPrescriptionDetail(prescription), "Thêm thuốc vào đơn thành công");
    }

    public Result<PrescriptionDetailDto> UpdatePrescriptionItem(int id, int itemId, PrescriptionItemRequest request)
    {
        var prescription = db.Prescriptions.FirstOrDefault(p => p.Id == id);
        if (prescription is null) return NotFound<PrescriptionDetailDto>("Không tìm thấy đơn thuốc");
        if (IsCurrentDoctor() && !CurrentDoctorOwns(prescription.DoctorId))
            return Forbidden<PrescriptionDetailDto>("Bác sĩ chỉ được sửa đơn thuốc thuộc mình");
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
        if (IsCurrentDoctor() && !CurrentDoctorOwns(prescription.DoctorId))
            return Forbidden<PrescriptionDetailDto>("Bác sĩ chỉ được sửa đơn thuốc thuộc mình");
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
            if (IsCurrentDoctor() && !CurrentDoctorOwns(recordToCreate.DoctorId))
                return Forbidden<PrescriptionSubmitDto>("Bác sĩ chỉ được chốt đơn thuốc thuộc mình");
            prescription = CreatePrescriptionEntity(recordToCreate, request.Note);
        }

        if (prescription is null) return NotFound<PrescriptionSubmitDto>("Không tìm thấy đơn thuốc");
        if (IsCurrentDoctor() && !CurrentDoctorOwns(prescription.DoctorId))
            return Forbidden<PrescriptionSubmitDto>("Bác sĩ chỉ được chốt đơn thuốc thuộc mình");

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
        if (IsCurrentDoctor() && !CurrentDoctorOwns(prescription.DoctorId))
            return Forbidden<PrescriptionDetailDto>("Bác sĩ chỉ được hủy đơn thuốc thuộc mình");

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
        if (IsCurrentDoctor() && !CurrentDoctorOwns(record.DoctorId))
            return Forbidden<ClinicalOrderDto>("Bác sĩ chỉ được tạo chỉ định cho bệnh án thuộc mình");
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
        var query = db.ClinicalOrders
            .AsNoTracking()
            .Where(o => medicalRecordId == null || o.MedicalRecordId == medicalRecordId)
            .Where(o => patientId == null || o.PatientId == patientId);

        if (IsCurrentPatient())
        {
            var currentPatientId = CurrentPatientIdFromClaims();
            if (currentPatientId is null) return Forbidden<IReadOnlyList<ClinicalOrderDto>>("Token bệnh nhân thiếu PatientId");
            query = query.Where(o => o.PatientId == currentPatientId.Value);
        }
        else if (IsCurrentDoctor())
        {
            var currentDoctorId = CurrentDoctorId();
            if (currentDoctorId is null) return Forbidden<IReadOnlyList<ClinicalOrderDto>>("Token bác sĩ thiếu DoctorId");
            query = query.Where(o => o.DoctorId == currentDoctorId.Value);
        }

        var orders = query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => ToClinicalOrderDto(o))
            .ToList();

        return Result<IReadOnlyList<ClinicalOrderDto>>.Ok(orders, "Lấy danh sách chỉ định lâm sàng thành công");
    }

    public Result<ClinicalOrderDto> UpdateClinicalOrderResult(int id, ClinicalOrderResultRequest request)
    {
        var order = db.ClinicalOrders.FirstOrDefault(o => o.Id == id);
        if (order is null) return NotFound<ClinicalOrderDto>("Không tìm thấy chỉ định lâm sàng");
        if (IsCurrentDoctor() && !CurrentDoctorOwns(order.DoctorId))
            return Forbidden<ClinicalOrderDto>("Bác sĩ chỉ được nhập kết quả chỉ định thuộc mình");
        if (IsCurrentPatient()) return Forbidden<ClinicalOrderDto>("Bệnh nhân không được nhập kết quả cận lâm sàng");

        order.ResultText = request.ResultText;
        order.ResultValue = request.ResultValue;
        order.ResultUnit = request.ResultUnit;
        order.ResultFileUrl = request.ResultFileUrl;
        order.Conclusion = request.Conclusion;
        order.ResultedBy = string.IsNullOrWhiteSpace(request.ResultedBy)
            ? CurrentDisplayName()
            : request.ResultedBy.Trim();
        order.ResultedAt = DateTime.UtcNow;
        order.Status = MedicalStatuses.Completed;
        db.SaveChanges();

        return Result<ClinicalOrderDto>.Ok(ToClinicalOrderDto(order), "Cập nhật kết quả cận lâm sàng thành công");
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
        snapshot.Reason = NormalizeOptionalText(request.Data.Reason);
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
            if (string.IsNullOrWhiteSpace(existingVisit.ChiefComplaint))
            {
                existingVisit.ChiefComplaint = NormalizeOptionalText(request.Data.Reason)
                    ?? NormalizeOptionalText(snapshot.Reason);
            }
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
            ChiefComplaint = NormalizeOptionalText(request.Data.Reason)
                ?? NormalizeOptionalText(snapshot.Reason),
            Status = isInProgress ? MedicalStatuses.InProgress : MedicalStatuses.WaitingForExam,
            StartedAt = isInProgress ? request.Data.CheckedInAt : null
        };

        db.Visits.Add(visit);
        AddInbox(request.Source, request.EventCode, request.EventType, JsonSerializer.Serialize(request));
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException exception) when (IsDuplicateVisitAppointment(exception))
        {
            db.ChangeTracker.Clear();
            existingVisit = db.Visits.AsNoTracking()
                .First(v => v.AppointmentId == request.Data.AppointmentId);
            return Result<EventResultDto>.Ok(
                new(request.EventCode, request.EventType, MedicalStatuses.Processed,
                    $"Lượt khám {existingVisit.VisitCode ?? $"#{existingVisit.Id}"} đã tồn tại"),
                "Event check-in đã được đồng bộ trước đó");
        }
        visit.VisitCode = $"LK{visit.Id:D3}";
        db.SaveChanges();

        return Result<EventResultDto>.Ok(new(request.EventCode, request.EventType, MedicalStatuses.Processed, $"Đã tạo lượt khám {visit.VisitCode}"), "Xử lý event check-in thành công");
    }

    private static bool IsInProgressEvent(string? status)
        => string.Equals(status, "InProgress", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, MedicalStatuses.InProgress, StringComparison.OrdinalIgnoreCase);

    private static bool IsDuplicateVisitAppointment(DbUpdateException exception)
        => exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Visits_AppointmentId"
        };

    public Result<IReadOnlyList<InboxEventDto>> GetInboxEvents(string? status, string? eventType)
    {
        var events = db.InboxEvents
            .AsNoTracking()
            .Where(e => string.IsNullOrWhiteSpace(status) || e.Status == status)
            .Where(e => string.IsNullOrWhiteSpace(eventType) || e.EventType == eventType)
            .OrderByDescending(e => e.ProcessedAt)
            .Select(e => ToInboxDto(e))
            .ToList();

        return Result<IReadOnlyList<InboxEventDto>>.Ok(events, "Lấy danh sách inbox event thành công");
    }

    public Result<IReadOnlyList<OutboxEventDto>> GetOutboxEvents(string? status, string? eventType)
    {
        var events = db.OutboxEvents
            .AsNoTracking()
            .Where(e => string.IsNullOrWhiteSpace(status) || e.Status == status)
            .Where(e => string.IsNullOrWhiteSpace(eventType) || e.EventType == eventType)
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

    public Result<OutboxEventDto> RetryOutboxEvent(int id)
    {
        var outbox = db.OutboxEvents.FirstOrDefault(e => e.Id == id);
        if (outbox is null) return NotFound<OutboxEventDto>("Không tìm thấy outbox event");

        outbox.Status = MedicalStatuses.PendingPublish;
        outbox.PublishedAt = null;
        outbox.ErrorMessage = null;
        outbox.RetryCount = Math.Max(0, outbox.RetryCount);
        db.SaveChanges();

        return Result<OutboxEventDto>.Ok(ToOutboxDto(outbox), "Đưa event về trạng thái chờ gửi thành công");
    }

    public Result<OutboxEventDto> FailOutboxEvent(int id)
    {
        var outbox = db.OutboxEvents.FirstOrDefault(e => e.Id == id);
        if (outbox is null) return NotFound<OutboxEventDto>("Không tìm thấy outbox event");

        outbox.Status = MedicalStatuses.Failed;
        outbox.ErrorMessage ??= "Đánh dấu thất bại thủ công";
        db.SaveChanges();

        return Result<OutboxEventDto>.Ok(ToOutboxDto(outbox), "Đánh dấu event thất bại thành công");
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

    private OutboxEvent CreateMedicalRecordOutbox(MedicalRecord record, string eventType)
    {
        var visit = db.Visits.AsNoTracking().First(v => v.Id == record.VisitId);
        var patient = db.Patients.AsNoTracking().First(p => p.Id == record.PatientId);
        var snapshot = visit.AppointmentId is null
            ? null
            : db.AppointmentSnapshots.AsNoTracking().FirstOrDefault(a => a.AppointmentId == visit.AppointmentId);

        var outbox = new OutboxEvent
        {
            EventType = eventType,
            AggregateType = nameof(MedicalRecord),
            AggregateId = record.Id,
            Payload = string.Empty
        };
        db.OutboxEvents.Add(outbox);
        db.SaveChanges();

        var eventCode = $"N2EV-MR-{outbox.Id:D3}";
        outbox.EventCode = eventCode;
        outbox.Payload = JsonSerializer.Serialize(new
        {
            eventCode,
            eventType,
            source = "MedicalRecordService",
            occurredAt = DateTime.UtcNow,
            data = new
            {
                medicalRecordId = record.Id,
                medicalRecordCode = record.MedicalRecordCode,
                visitId = record.VisitId,
                appointmentId = visit.AppointmentId,
                patientId = patient.Id,
                patientCode = patient.PatientCode,
                patientName = patient.FullName,
                doctorId = record.DoctorId,
                doctorName = snapshot?.DoctorNameSnapshot,
                diagnosis = record.DiagnosisText,
                status = record.Status
            }
        });
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

    private int? ResolvePatientId(string patientKey, int? currentUserId, int? currentPatientId, string? currentEmail, string? currentFullName)
    {
        var normalized = patientKey.Trim();

        if (int.TryParse(normalized, out var numericId))
        {
            if (FindPatient(numericId) is not null)
            {
                return numericId;
            }

            return numericId;
        }

        if (normalized.StartsWith("BN", StringComparison.OrdinalIgnoreCase))
        {
            var patient = db.Patients
                .AsNoTracking()
                .FirstOrDefault(p => p.PatientCode != null
                    && p.PatientCode.ToUpper() == normalized.ToUpper()
                    && !p.IsDeleted);

            if (patient is not null)
            {
                return patient.Id;
            }

            if (int.TryParse(normalized[2..], out var codeId))
            {
                return codeId;
            }
        }

        return null;
    }

    private Patient? FindPatientByIdentity(string? email, string? fullName)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = email.Trim().ToLower();
            var patientByEmail = db.Patients
                .AsNoTracking()
                .FirstOrDefault(p => p.Email != null && p.Email.ToLower() == normalizedEmail && !p.IsDeleted);

            if (patientByEmail is not null)
            {
                return patientByEmail;
            }
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        var normalizedName = fullName.Trim().ToLower();
        return db.Patients
            .AsNoTracking()
            .FirstOrDefault(p => p.FullName.ToLower() == normalizedName && !p.IsDeleted);
    }

    private bool IsCurrentPatient() => IsInRole("Patient");
    private bool IsCurrentDoctor() => IsInRole("Doctor");
    private bool IsCurrentAdmin() => IsInRole("Admin");
    private bool IsCurrentNurse() => IsInRole("Nurse");
    private bool IsCurrentReceptionist() => IsInRole("Receptionist");
    private bool IsCurrentStaffReader() => IsCurrentAdmin() || IsCurrentNurse() || IsCurrentReceptionist();

    private bool IsInRole(string role)
        => httpContextAccessor.HttpContext?.User?.IsInRole(role) == true;

    private int? CurrentPatientIdFromClaims()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var value = user?.FindFirst("PatientId")?.Value ?? user?.FindFirst("patientId")?.Value;
        return int.TryParse(value, out var patientId) ? patientId : null;
    }

    private int? CurrentUserIdFromClaims()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var value = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("sub")?.Value;
        return int.TryParse(value, out var userId) ? userId : null;
    }

    private string? CurrentDisplayName()
    {
        var user = httpContextAccessor.HttpContext?.User;
        return user?.FindFirst(ClaimTypes.Name)?.Value
            ?? user?.FindFirst("unique_name")?.Value
            ?? user?.FindFirst("Username")?.Value
            ?? user?.Identity?.Name;
    }

    private int? CurrentDoctorId()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var claimValue = user?.FindFirst("DoctorId")?.Value ?? user?.FindFirst("doctorId")?.Value;
        if (int.TryParse(claimValue, out var claimDoctorId))
        {
            return claimDoctorId;
        }

        var userId = CurrentUserIdFromClaims();
        if (userId is null)
        {
            return null;
        }

        try
        {
            var appointmentBaseUrl = configuration["ServiceUrls:AppointmentService"] ?? "http://appointment-service:8080";
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{appointmentBaseUrl.TrimEnd('/')}/api/doctors/by-user/{userId.Value}");
            CopyAuthorizationHeader(request);

            using var response = httpClientFactory.CreateClient().Send(request);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Không resolve được DoctorId từ N1 cho UserId {UserId}. Status {StatusCode}.", userId, response.StatusCode);
                return null;
            }

            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var json = JsonDocument.Parse(body);
            return TryGetInt(json.RootElement, "doctorId")
                ?? TryGetInt(json.RootElement, "id")
                ?? (json.RootElement.TryGetProperty("data", out var data)
                    ? TryGetInt(data, "doctorId") ?? TryGetInt(data, "id")
                    : null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không resolve được DoctorId từ N1 cho UserId {UserId}.", userId);
            return null;
        }
    }

    private static int? TryGetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt32(out var value) => value,
            JsonValueKind.String when int.TryParse(property.GetString(), out var value) => value,
            _ => null
        };
    }

    private bool CurrentDoctorOwns(int doctorId)
        => CurrentDoctorId() is int currentDoctorId && currentDoctorId == doctorId;

    private Result<T>? EnsurePatientReadAccess<T>(int patientId)
    {
        if (IsCurrentStaffReader())
        {
            return null;
        }

        if (IsCurrentPatient())
        {
            var currentPatientId = CurrentPatientIdFromClaims();
            return currentPatientId == patientId
                ? null
                : Forbidden<T>("Bệnh nhân chỉ được xem dữ liệu của chính mình");
        }

        if (IsCurrentDoctor())
        {
            var currentDoctorId = CurrentDoctorId();
            if (currentDoctorId is null) return Forbidden<T>("Token bác sĩ thiếu DoctorId");

            var hasRelationship =
                db.Visits.AsNoTracking().Any(v => v.PatientId == patientId && v.DoctorId == currentDoctorId.Value)
                || db.MedicalRecords.AsNoTracking().Any(r => r.PatientId == patientId && r.DoctorId == currentDoctorId.Value)
                || db.Prescriptions.AsNoTracking().Any(p => p.PatientId == patientId && p.DoctorId == currentDoctorId.Value)
                || db.ClinicalOrders.AsNoTracking().Any(o => o.PatientId == patientId && o.DoctorId == currentDoctorId.Value);

            return hasRelationship ? null : Forbidden<T>("Bác sĩ chỉ được xem bệnh nhân thuộc lượt khám của mình");
        }

        return Forbidden<T>("Không có quyền truy cập dữ liệu bệnh nhân");
    }

    private Result<T>? EnsureVisitReadAccess<T>(Visit visit)
    {
        if (IsCurrentStaffReader()) return null;
        if (IsCurrentPatient())
        {
            return CurrentPatientIdFromClaims() == visit.PatientId
                ? null
                : Forbidden<T>("Bệnh nhân chỉ được xem lượt khám của chính mình");
        }

        if (IsCurrentDoctor())
        {
            return CurrentDoctorOwns(visit.DoctorId)
                ? null
                : Forbidden<T>("Bác sĩ chỉ được xem lượt khám thuộc mình");
        }

        return Forbidden<T>("Không có quyền truy cập lượt khám");
    }

    private Result<T>? EnsureRecordReadAccess<T>(MedicalRecord record)
    {
        if (IsCurrentStaffReader()) return null;
        if (IsCurrentPatient())
        {
            return CurrentPatientIdFromClaims() == record.PatientId
                ? null
                : Forbidden<T>("Bệnh nhân chỉ được xem bệnh án của chính mình");
        }

        if (IsCurrentDoctor())
        {
            return CurrentDoctorOwns(record.DoctorId)
                ? null
                : Forbidden<T>("Bác sĩ chỉ được xem bệnh án thuộc mình");
        }

        return Forbidden<T>("Không có quyền truy cập bệnh án");
    }

    private Result<T>? EnsurePrescriptionReadAccess<T>(Prescription prescription)
    {
        if (IsCurrentStaffReader()) return null;
        if (IsCurrentPatient())
        {
            return CurrentPatientIdFromClaims() == prescription.PatientId
                ? null
                : Forbidden<T>("Bệnh nhân chỉ được xem đơn thuốc của chính mình");
        }

        if (IsCurrentDoctor())
        {
            return CurrentDoctorOwns(prescription.DoctorId)
                ? null
                : Forbidden<T>("Bác sĩ chỉ được xem đơn thuốc thuộc mình");
        }

        return Forbidden<T>("Không có quyền truy cập đơn thuốc");
    }

    private ClinicalVisitBundleDto BuildVisitBundle(Visit visit)
    {
        var record = db.MedicalRecords.AsNoTracking().FirstOrDefault(r => r.VisitId == visit.Id);
        var orders = record is null
            ? []
            : db.ClinicalOrders.AsNoTracking()
                .Where(o => o.MedicalRecordId == record.Id)
                .OrderBy(o => o.CreatedAt)
                .Select(ToClinicalOrderDto)
                .ToList();
        var prescriptions = record is null
            ? []
            : db.Prescriptions.AsNoTracking()
                .Where(p => p.MedicalRecordId == record.Id)
                .OrderBy(p => p.CreatedAt)
                .ToList()
                .Select(ToPrescriptionDetail)
                .ToList();

        return new(
            ToAppointmentSnapshotDto(visit.AppointmentId),
            ToVisitDetail(visit),
            record is null ? null : ToMedicalRecordDetail(record),
            orders,
            prescriptions,
            BillingPlaceholder(visit.AppointmentId));
    }

    private CompleteMedicalRecordDto BuildCompleteMedicalRecord(Patient patient, Visit visit, MedicalRecord record)
    {
        var orders = db.ClinicalOrders.AsNoTracking()
            .Where(o => o.MedicalRecordId == record.Id)
            .OrderBy(o => o.CreatedAt)
            .Select(ToClinicalOrderDto)
            .ToList();
        var prescriptions = db.Prescriptions.AsNoTracking()
            .Where(p => p.MedicalRecordId == record.Id)
            .OrderBy(p => p.CreatedAt)
            .ToList()
            .Select(ToPrescriptionDetail)
            .ToList();

        return new(
            ToDetail(patient),
            ToAppointmentSnapshotDto(visit.AppointmentId),
            ToVisitDetail(visit),
            ToMedicalRecordDetail(record),
            orders,
            prescriptions,
            BillingPlaceholder(visit.AppointmentId));
    }

    private AppointmentSnapshotDto? ToAppointmentSnapshotDto(int? appointmentId)
    {
        if (appointmentId is null) return null;
        var snapshot = db.AppointmentSnapshots.AsNoTracking().FirstOrDefault(a => a.AppointmentId == appointmentId.Value);
        return snapshot is null
            ? null
            : new(snapshot.AppointmentId, snapshot.PatientId, snapshot.PatientNameSnapshot, snapshot.DoctorId,
                snapshot.DoctorNameSnapshot, snapshot.SpecialtyId, snapshot.SpecialtyNameSnapshot, snapshot.ScheduledAt,
                snapshot.QueueNumber, snapshot.Status, snapshot.ConfirmedAt, snapshot.CreatedAt, snapshot.Reason);
    }

    private static BillingSummaryDto BillingPlaceholder(int? appointmentId)
        => new(
            "N3",
            "External",
            appointmentId is null
                ? "Viện phí thuộc N3 Pharmacy/Billing Service, không có AppointmentId để đối chiếu."
                : "Viện phí thuộc N3 Pharmacy/Billing Service. Frontend gọi N3 theo quyền JWT để lấy hóa đơn.",
            null,
            null,
            null);

    private static string BuildRecordHtml(CompleteMedicalRecordDto record)
    {
        static string E(string? value) => WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "-" : value);
        static string D(DateTime? value) => value?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "-";
        static string Date(DateOnly? value) => value?.ToString("dd/MM/yyyy") ?? "-";

        var html = new StringBuilder();
        html.Append("""
<!doctype html>
<html lang="vi">
<head>
  <meta charset="utf-8">
  <title>Hồ sơ bệnh án MedicareDNU</title>
  <style>
    body{font-family:Arial,sans-serif;color:#111827;margin:32px;line-height:1.5}
    h1,h2{color:#0f4c9a} table{border-collapse:collapse;width:100%;margin:12px 0 24px}
    td,th{border:1px solid #d6deea;padding:8px;text-align:left;vertical-align:top}
    th{background:#eef5ff}.muted{color:#64748b}.section{margin-top:24px}
  </style>
</head>
<body>
""");
        html.Append($"<h1>Hồ sơ bệnh án {E(record.MedicalRecord.MedicalRecordCode)}</h1>");
        html.Append($"<p class=\"muted\">Xuất lúc {D(DateTime.UtcNow)}</p>");
        html.Append("<h2>Thông tin bệnh nhân</h2><table>");
        html.Append($"<tr><th>Mã BN</th><td>{E(record.Patient.PatientCode)}</td><th>Họ tên</th><td>{E(record.Patient.FullName)}</td></tr>");
        html.Append($"<tr><th>Ngày sinh</th><td>{Date(record.Patient.DateOfBirth)}</td><th>Giới tính</th><td>{E(record.Patient.Gender)}</td></tr>");
        html.Append($"<tr><th>CCCD</th><td>{E(record.Patient.CitizenId)}</td><th>SĐT</th><td>{E(record.Patient.PhoneNumber)}</td></tr>");
        html.Append($"<tr><th>Email</th><td>{E(record.Patient.Email)}</td><th>Địa chỉ</th><td>{E(record.Patient.Address)}</td></tr>");
        html.Append("</table>");

        html.Append("<h2>Lượt khám</h2><table>");
        html.Append($"<tr><th>Mã lượt</th><td>{E(record.Visit.VisitCode)}</td><th>Lịch hẹn</th><td>{E(record.Visit.AppointmentId?.ToString())}</td></tr>");
        html.Append($"<tr><th>Bác sĩ</th><td>{E(record.Visit.DoctorName)} (ID {record.Visit.DoctorId})</td><th>Ngày khám</th><td>{D(record.Visit.VisitDate)}</td></tr>");
        html.Append($"<tr><th>Lý do khám</th><td colspan=\"3\">{E(record.Visit.ChiefComplaint)}</td></tr>");
        html.Append($"<tr><th>Triệu chứng</th><td colspan=\"3\">{E(record.Visit.Symptoms)}</td></tr>");
        html.Append($"<tr><th>Sinh hiệu</th><td colspan=\"3\"><pre>{E(record.Visit.VitalSignsJson)}</pre></td></tr>");
        html.Append("</table>");

        html.Append("<h2>Chẩn đoán và điều trị</h2><table>");
        html.Append($"<tr><th>Mã ICD</th><td>{E(record.MedicalRecord.DiagnosisCode)}</td><th>Trạng thái</th><td>{E(record.MedicalRecord.Status)}</td></tr>");
        html.Append($"<tr><th>Chẩn đoán</th><td colspan=\"3\">{E(record.MedicalRecord.DiagnosisText)}</td></tr>");
        html.Append($"<tr><th>Ghi chú bác sĩ</th><td colspan=\"3\">{E(record.MedicalRecord.DoctorNote)}</td></tr>");
        html.Append($"<tr><th>Kế hoạch điều trị</th><td colspan=\"3\">{E(record.MedicalRecord.TreatmentPlan)}</td></tr>");
        html.Append($"<tr><th>Tái khám</th><td colspan=\"3\">{Date(record.MedicalRecord.FollowUpDate)}</td></tr>");
        html.Append("</table>");

        html.Append("<h2>Cận lâm sàng</h2><table><tr><th>Mã</th><th>Loại</th><th>Tên</th><th>Kết quả</th><th>Kết luận</th><th>Ngày trả</th></tr>");
        foreach (var order in record.ClinicalOrders)
        {
            html.Append($"<tr><td>{E(order.ClinicalOrderCode)}</td><td>{E(order.OrderType)}</td><td>{E(order.OrderName)}</td><td>{E(order.ResultText ?? $"{order.ResultValue} {order.ResultUnit}")}</td><td>{E(order.Conclusion)}</td><td>{D(order.ResultedAt)}</td></tr>");
        }
        html.Append("</table>");

        html.Append("<h2>Đơn thuốc</h2>");
        foreach (var prescription in record.Prescriptions)
        {
            html.Append($"<h3>{E(prescription.PrescriptionCode)} - {E(prescription.Status)}</h3>");
            html.Append("<table><tr><th>Thuốc</th><th>Liều</th><th>Tần suất</th><th>Số ngày</th><th>Số lượng</th><th>Hướng dẫn</th></tr>");
            foreach (var item in prescription.Items)
            {
                html.Append($"<tr><td>{E(item.MedicineNameSnapshot)} ({E(item.UnitSnapshot)})</td><td>{E(item.Dosage)}</td><td>{E(item.Frequency)}</td><td>{item.DurationDays}</td><td>{item.Quantity}</td><td>{E(item.UsageInstruction)}</td></tr>");
            }
            html.Append("</table>");
        }

        html.Append($"<p class=\"muted\">Viện phí: {E(record.Billing.Message)}</p>");
        html.Append("</body></html>");
        return html.ToString();
    }

    private static PatientSummaryDto ToSummary(Patient patient)
        => new(patient.Id, patient.PatientCode, patient.FullName, patient.PhoneNumber, patient.Status);

    private static PatientLookupDto ToLookup(Patient patient)
        => new(patient.Id, patient.PatientCode, patient.FullName, patient.PhoneNumber, patient.DateOfBirth, patient.Gender, patient.Status);

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
            visit.DoctorId, snapshot?.DoctorNameSnapshot, visit.VisitDate,
            NormalizeOptionalText(visit.ChiefComplaint) ?? NormalizeOptionalText(snapshot?.Reason),
            visit.Symptoms, visit.VitalSignsJson,
            visit.Status, visit.StartedAt, visit.CompletedAt, visit.CancelReason);
    }

    private MedicalRecordDetailDto ToMedicalRecordDetail(MedicalRecord record)
    {
        var patient = db.Patients.AsNoTracking().FirstOrDefault(p => p.Id == record.PatientId);
        return new(record.Id, record.MedicalRecordCode, record.VisitId, record.PatientId, patient?.PatientCode, record.DoctorId,
            record.DiagnosisCode, record.DiagnosisSpecialty, record.DiagnosisText, record.DoctorNote, record.TreatmentPlan, record.FollowUpDate,
            record.Status, record.CreatedAt, record.UpdatedAt, record.CompletedAt);
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
            order.OrderType, order.OrderName, order.Reason, order.Status, order.CreatedAt,
            order.ResultText, order.ResultValue, order.ResultUnit, order.ResultFileUrl, order.Conclusion, order.ResultedAt, order.ResultedBy);

    private static InboxEventDto ToInboxDto(InboxEvent e)
        => new(e.Id, e.EventCode, e.SourceService, e.EventType, e.Payload ?? string.Empty, e.ProcessedAt, e.Status, e.ErrorMessage);

    private static OutboxEventDto ToOutboxDto(OutboxEvent e)
        => new(e.Id, e.EventCode, e.EventType, e.AggregateType, e.AggregateId, e.Payload, e.Status, e.OccurredAt, e.PublishedAt, e.RetryCount, e.ErrorMessage);

    private static string NormalizeLookupText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd');
    }

    private static string CapitalizeFullName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        var words = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
                words[i] = char.ToUpper(words[i][0]) + words[i][1..].ToLower();
        }
        return string.Join(' ', words);
    }

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool HasMeasuredVitalSigns(VisitVitalsRequest request)
        => request.Temperature.HasValue
            || !string.IsNullOrWhiteSpace(request.BloodPressure)
            || request.HeartRate.HasValue
            || request.RespiratoryRate.HasValue
            || request.Spo2.HasValue
            || request.Weight.HasValue
            || request.Height.HasValue;

    private static bool HasMeasuredVitalSigns(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            var vitals = JsonSerializer.Deserialize<VisitVitalsRequest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return vitals is not null && HasMeasuredVitalSigns(vitals);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static Result<T> NotFound<T>(string message)
        => Result<T>.Fail(message, StatusCodes.Status404NotFound, new ApiError("id", "NOT_FOUND", message));

    private static Result<T> Conflict<T>(string message)
        => Result<T>.Fail(message, StatusCodes.Status409Conflict, new ApiError("state", "CONFLICT", message));

    private static Result<T> Invalid<T>(string message, string field, string code, string errorMessage)
        => Result<T>.Fail(message, StatusCodes.Status400BadRequest, new ApiError(field, code, errorMessage));

    private static Result<T> Forbidden<T>(string message)
        => Result<T>.Fail(message, StatusCodes.Status403Forbidden, new ApiError("authorization", "FORBIDDEN", message));
}
