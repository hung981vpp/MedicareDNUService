using MedicalAPI.Application.DTOs;
using MedicalAPI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAPI.Controllers;

[Route("api/v1/medical/records")]
public sealed class MedicalRecordsController(IMedicalRecordService service) : MedicalControllerBase
{
    [HttpPost]
    [EndpointSummary("Tạo bệnh án")]
    [EndpointDescription("Tạo bệnh án điện tử cho lượt khám đang ở trạng thái Đang khám và sinh mã BA001.")]
    public IActionResult Create(MedicalRecordCreateRequest request) => ToActionResult(service.CreateMedicalRecord(request));

    [HttpGet("{id:int}")]
    [EndpointSummary("Xem chi tiết bệnh án")]
    [EndpointDescription("Lấy thông tin chẩn đoán, ghi chú bác sĩ, hướng điều trị và trạng thái bệnh án.")]
    public IActionResult GetById(int id) => ToActionResult(service.GetMedicalRecord(id));

    [HttpGet("by-visit/{visitId:int}")]
    [EndpointSummary("Lấy bệnh án theo lượt khám")]
    [EndpointDescription("Tìm bệnh án chính gắn với một lượt khám cụ thể.")]
    public IActionResult GetByVisit(int visitId) => ToActionResult(service.GetMedicalRecordByVisit(visitId));

    [HttpPut("{id:int}")]
    [EndpointSummary("Cập nhật bệnh án nháp")]
    [EndpointDescription("Cập nhật bệnh án khi bệnh án vẫn ở trạng thái Bản nháp.")]
    public IActionResult Update(int id, MedicalRecordUpdateRequest request) => ToActionResult(service.UpdateMedicalRecord(id, request));

    [HttpPut("{id:int}/complete")]
    [EndpointSummary("Hoàn tất bệnh án")]
    [EndpointDescription("Chuyển bệnh án sang trạng thái Đã hoàn tất sau khi đã có chẩn đoán hợp lệ.")]
    public IActionResult Complete(int id) => ToActionResult(service.CompleteMedicalRecord(id));
}
