using MedicalAPI.Application.DTOs;
using MedicalAPI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAPI.Controllers;

[Route("api/v1/medical/patients")]
public sealed class PatientsController(IMedicalRecordService service) : MedicalControllerBase
{
    [HttpGet]
    [EndpointSummary("Danh sách bệnh nhân")]
    [EndpointDescription("Lấy danh sách hồ sơ bệnh nhân có phân trang. Có thể lọc theo tên, mã bệnh nhân hoặc số điện thoại bằng keyword.")]
    public IActionResult Search(
        [FromQuery] string? keyword,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
        => ToActionResult(service.SearchPatients(keyword, pageNumber, pageSize));

    [HttpGet("{id:int}")]
    [EndpointSummary("Xem chi tiết bệnh nhân")]
    [EndpointDescription("Lấy thông tin đầy đủ của một hồ sơ bệnh nhân theo ID.")]
    public IActionResult GetById(int id) => ToActionResult(service.GetPatient(id));

    [HttpPost]
    [EndpointSummary("Tạo hồ sơ bệnh nhân")]
    [EndpointDescription("Tạo hồ sơ bệnh nhân mới và sinh mã bệnh nhân dạng BN001.")]
    public IActionResult Create(PatientCreateRequest request) => ToActionResult(service.CreatePatient(request));

    [HttpPut("{id:int}")]
    [EndpointSummary("Cập nhật hồ sơ bệnh nhân")]
    [EndpointDescription("Cập nhật thông tin hành chính, tiền sử bệnh, dị ứng và trạng thái hồ sơ bệnh nhân.")]
    public IActionResult Update(int id, PatientUpdateRequest request) => ToActionResult(service.UpdatePatient(id, request));

    [HttpGet("{id:int}/history")]
    [EndpointSummary("Xem lịch sử khám")]
    [EndpointDescription("Lấy lịch sử lượt khám, bệnh án và đơn thuốc của một bệnh nhân.")]
    public IActionResult History(int id) => ToActionResult(service.GetPatientHistory(id));
}
