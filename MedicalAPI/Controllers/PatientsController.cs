using MedicalAPI.Application.DTOs;
using MedicalAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAPI.Controllers;

[Route("api/v1/medical/patients")]
public sealed class PatientsController(IMedicalRecordService service) : MedicalControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    [EndpointSummary("Danh sách bệnh nhân")]
    [EndpointDescription("Lấy danh sách hồ sơ bệnh nhân có phân trang. Có thể lọc theo tên, mã bệnh nhân hoặc số điện thoại bằng keyword.")]
    public IActionResult Search(
        [FromQuery] string? keyword,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
        => ToActionResult(service.SearchPatients(keyword, pageNumber, pageSize));

    [HttpGet("me")]
    [Authorize(Roles = "Patient")]
    [EndpointSummary("Hồ sơ bệnh nhân của tôi")]
    [EndpointDescription("Lấy hồ sơ bệnh nhân theo PatientId trong JWT, không nhận id từ frontend.")]
    public IActionResult Me() => ToActionResult(service.GetCurrentPatient());

    [HttpGet("me/history")]
    [Authorize(Roles = "Patient")]
    [EndpointSummary("Lịch sử khám của tôi")]
    [EndpointDescription("Lấy lịch sử khám theo PatientId trong JWT, không dò theo tên/email/số điện thoại.")]
    public IActionResult MyHistory() => ToActionResult(service.GetCurrentPatientHistory());

    [HttpGet("me/clinical-timeline")]
    [Authorize(Roles = "Patient")]
    [EndpointSummary("Timeline lâm sàng của tôi")]
    [EndpointDescription("Gom lượt khám, bệnh án, chỉ định, đơn thuốc và placeholder viện phí theo PatientId trong JWT.")]
    public IActionResult MyClinicalTimeline() => ToActionResult(service.GetCurrentPatientClinicalTimeline());

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    [EndpointSummary("Xem chi tiết bệnh nhân")]
    [EndpointDescription("Lấy thông tin đầy đủ của một hồ sơ bệnh nhân theo ID.")]
    public IActionResult GetById(int id) => ToActionResult(service.GetPatientByKey(PatientScopedKey(id.ToString()), CurrentUserId(), CurrentPatientId(), CurrentEmail(), CurrentFullName()));

    [HttpGet("{patientKey}")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    [EndpointSummary("Xem chi tiết bệnh nhân theo mã")]
    [EndpointDescription("Lấy thông tin đầy đủ của một hồ sơ bệnh nhân theo PatientId, UserId của tài khoản bệnh nhân, hoặc mã BNxxx.")]
    public IActionResult GetByKey(string patientKey) => ToActionResult(service.GetPatientByKey(PatientScopedKey(patientKey), CurrentUserId(), CurrentPatientId(), CurrentEmail(), CurrentFullName()));

    [HttpPost]
    [Authorize(Roles = "Admin,Nurse,Receptionist")]
    [EndpointSummary("Tạo hồ sơ bệnh nhân")]
    [EndpointDescription("Tạo hồ sơ bệnh nhân mới và sinh mã bệnh nhân dạng BN001.")]
    public IActionResult Create(PatientCreateRequest request) => ToActionResult(service.CreatePatient(request));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Nurse,Receptionist,Patient")]
    [EndpointSummary("Cập nhật hồ sơ bệnh nhân")]
    [EndpointDescription("Cập nhật thông tin hành chính, tiền sử bệnh, dị ứng và trạng thái hồ sơ bệnh nhân.")]
    public IActionResult Update(int id, PatientUpdateRequest request) => ToActionResult(service.UpdatePatient(id, request));

    [HttpPut("me")]
    [Authorize(Roles = "Patient")]
    [EndpointSummary("Cập nhật hồ sơ bệnh nhân của tôi")]
    [EndpointDescription("Bệnh nhân cập nhật thông tin hành chính, tiền sử bệnh và dị ứng theo PatientId trong JWT.")]
    public IActionResult UpdateMe(PatientUpdateRequest request) => ToActionResult(service.UpdateCurrentPatient(request));

    [HttpGet("{id:int}/history")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    [EndpointSummary("Xem lịch sử khám")]
    [EndpointDescription("Lấy lịch sử lượt khám, bệnh án và đơn thuốc của một bệnh nhân.")]
    public IActionResult History(int id) => ToActionResult(service.GetPatientHistoryByKey(PatientScopedKey(id.ToString()), CurrentUserId(), CurrentPatientId(), CurrentEmail(), CurrentFullName()));

    [HttpGet("{patientKey}/history")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    [EndpointSummary("Xem lịch sử khám theo mã")]
    [EndpointDescription("Lấy lịch sử lượt khám, bệnh án và đơn thuốc theo PatientId, UserId của tài khoản bệnh nhân, hoặc mã BNxxx.")]
    public IActionResult HistoryByKey(string patientKey) => ToActionResult(service.GetPatientHistoryByKey(PatientScopedKey(patientKey), CurrentUserId(), CurrentPatientId(), CurrentEmail(), CurrentFullName()));

    [HttpGet("{id:int}/clinical-timeline")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    [EndpointSummary("Timeline lâm sàng theo bệnh nhân")]
    [EndpointDescription("Gom lượt khám, bệnh án, chỉ định, đơn thuốc và placeholder viện phí theo PatientId, có kiểm tra ownership.")]
    public IActionResult ClinicalTimeline(int id) => ToActionResult(service.GetPatientClinicalTimeline(id));

    private static string PatientScopedKey(string requestedKey) => requestedKey;
}
