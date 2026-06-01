using MedicalAPI.Application.DTOs;
using MedicalAPI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAPI.Controllers;

[Route("api/v1/medical/clinical-orders")]
public sealed class ClinicalOrdersController(IMedicalRecordService service) : MedicalControllerBase
{
    [HttpGet]
    [EndpointSummary("Lấy danh sách chỉ định lâm sàng")]
    [EndpointDescription("Lấy danh sách chỉ định xét nghiệm, siêu âm, X-quang hoặc chỉ định khác theo bệnh án hoặc bệnh nhân.")]
    public IActionResult Search([FromQuery] int? medicalRecordId, [FromQuery] int? patientId)
        => ToActionResult(service.GetClinicalOrders(medicalRecordId, patientId));

    [HttpPost]
    [EndpointSummary("Tạo chỉ định lâm sàng")]
    [EndpointDescription("Tạo chỉ định lâm sàng cho bệnh án và sinh mã chỉ định dạng CD001.")]
    public IActionResult Create(ClinicalOrderCreateRequest request) => ToActionResult(service.CreateClinicalOrder(request));
}
