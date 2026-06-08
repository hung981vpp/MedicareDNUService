using MedicalAPI.Application.DTOs;
using MedicalAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAPI.Controllers;

[Route("api/v1/medical/clinical-orders")]
public sealed class ClinicalOrdersController(IMedicalRecordService service) : MedicalControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    [EndpointSummary("Lấy danh sách chỉ định lâm sàng")]
    [EndpointDescription("Lấy danh sách chỉ định xét nghiệm, siêu âm, X-quang hoặc chỉ định khác theo bệnh án hoặc bệnh nhân.")]
    public IActionResult Search([FromQuery] int? medicalRecordId, [FromQuery] int? patientId)
    {
        if (IsPatient())
        {
            patientId = CurrentPatientId();
            if (patientId is null)
            {
                return Forbid();
            }
        }

        return ToActionResult(service.GetClinicalOrders(medicalRecordId, patientId));
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [EndpointSummary("Tạo chỉ định lâm sàng")]
    [EndpointDescription("Tạo chỉ định lâm sàng cho bệnh án và sinh mã chỉ định dạng CD001.")]
    public IActionResult Create(ClinicalOrderCreateRequest request) => ToActionResult(service.CreateClinicalOrder(request));

    [HttpPut("{id:int}/result")]
    [Authorize(Roles = "Doctor,Nurse")]
    [EndpointSummary("Nhập kết quả cận lâm sàng")]
    [EndpointDescription("Cập nhật kết quả xét nghiệm/siêu âm/X-quang/ECG và chuyển chỉ định sang trạng thái hoàn tất.")]
    public IActionResult UpdateResult(int id, ClinicalOrderResultRequest request)
        => ToActionResult(service.UpdateClinicalOrderResult(id, request));
}
