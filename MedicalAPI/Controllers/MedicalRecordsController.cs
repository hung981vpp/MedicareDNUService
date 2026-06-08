using MedicalAPI.Application.DTOs;
using MedicalAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAPI.Controllers;

[Route("api/v1/medical/records")]
public sealed class MedicalRecordsController(IMedicalRecordService service) : MedicalControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [EndpointSummary("Tạo bệnh án")]
    [EndpointDescription("Tạo bệnh án điện tử cho lượt khám đang ở trạng thái Đang khám và sinh mã BA001.")]
    public IActionResult Create(MedicalRecordCreateRequest request) => ToActionResult(service.CreateMedicalRecord(request));

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    [EndpointSummary("Xem chi tiết bệnh án")]
    [EndpointDescription("Lấy thông tin chẩn đoán, ghi chú bác sĩ, hướng điều trị và trạng thái bệnh án.")]
    public IActionResult GetById(int id)
        => ToActionResult(service.GetMedicalRecord(id));

    [HttpGet("{id:int}/complete")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    [EndpointSummary("Xem bệnh án hoàn chỉnh")]
    [EndpointDescription("Gom thông tin bệnh nhân, lượt khám, sinh hiệu, bệnh án, chỉ định/kết quả, đơn thuốc và placeholder viện phí.")]
    public IActionResult CompleteRecord(int id)
        => ToActionResult(service.GetCompleteMedicalRecord(id));

    [HttpGet("{id:int}/export/html")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    [EndpointSummary("Xuất bệnh án HTML")]
    [EndpointDescription("Xuất hồ sơ bệnh án chính thức dạng HTML, có kiểm tra quyền sở hữu.")]
    public IActionResult ExportHtml(int id)
    {
        var result = service.ExportMedicalRecordHtml(id);
        return result.IsSuccess
            ? Content(result.Data ?? string.Empty, "text/html; charset=utf-8")
            : ToActionResult(result);
    }

    [HttpGet("by-visit/{visitId:int}")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    [EndpointSummary("Lấy bệnh án theo lượt khám")]
    [EndpointDescription("Tìm bệnh án chính gắn với một lượt khám cụ thể.")]
    public IActionResult GetByVisit(int visitId)
        => ToActionResult(service.GetMedicalRecordByVisit(visitId));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Doctor")]
    [EndpointSummary("Cập nhật bệnh án nháp")]
    [EndpointDescription("Cập nhật bệnh án khi bệnh án vẫn ở trạng thái Bản nháp.")]
    public IActionResult Update(int id, MedicalRecordUpdateRequest request) => ToActionResult(service.UpdateMedicalRecord(id, request));

    [HttpPut("{id:int}/complete")]
    [Authorize(Roles = "Doctor")]
    [EndpointSummary("Hoàn tất bệnh án")]
    [EndpointDescription("Chuyển bệnh án sang trạng thái Đã hoàn tất sau khi đã có chẩn đoán hợp lệ.")]
    public IActionResult Complete(int id) => ToActionResult(service.CompleteMedicalRecord(id));
}
