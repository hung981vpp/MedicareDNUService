using MedicalAPI.Application.DTOs;
using MedicalAPI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAPI.Controllers;

[Route("api/v1/medical/prescriptions")]
public sealed class PrescriptionsController(IMedicalRecordService service) : MedicalControllerBase
{
    [HttpPost]
    [EndpointSummary("Tạo đơn thuốc nháp")]
    [EndpointDescription("Tạo đơn thuốc gắn với bệnh án và sinh mã đơn thuốc dạng DT001.")]
    public IActionResult Create(PrescriptionCreateRequest request) => ToActionResult(service.CreatePrescription(request));

    [HttpGet("{id:int}")]
    [EndpointSummary("Xem chi tiết đơn thuốc")]
    [EndpointDescription("Lấy thông tin đơn thuốc cùng danh sách thuốc đã kê.")]
    public IActionResult GetById(int id) => ToActionResult(service.GetPrescription(id));

    [HttpPost("{id:int}/items")]
    [EndpointSummary("Thêm thuốc vào đơn")]
    [EndpointDescription("Thêm một dòng thuốc vào đơn, kiểm tra số lượng và số ngày dùng phải lớn hơn 0.")]
    public IActionResult AddItem(int id, PrescriptionItemRequest request) => ToActionResult(service.AddPrescriptionItem(id, request));

    [HttpPut("{id:int}/items/{itemId:int}")]
    [EndpointSummary("Cập nhật thuốc trong đơn")]
    [EndpointDescription("Cập nhật thông tin thuốc, liều dùng, tần suất, số ngày dùng và số lượng trong đơn thuốc.")]
    public IActionResult UpdateItem(int id, int itemId, PrescriptionItemRequest request) => ToActionResult(service.UpdatePrescriptionItem(id, itemId, request));

    [HttpDelete("{id:int}/items/{itemId:int}")]
    [EndpointSummary("Xóa thuốc khỏi đơn")]
    [EndpointDescription("Xóa một dòng thuốc khỏi đơn thuốc nháp.")]
    public IActionResult DeleteItem(int id, int itemId) => ToActionResult(service.DeletePrescriptionItem(id, itemId));

    [HttpPut("{id:int}/submit")]
    [EndpointSummary("Chốt đơn thuốc")]
    [EndpointDescription("Chốt đơn thuốc, chuyển trạng thái Đã gửi nhà thuốc và tạo outbox event prescription.created gửi sang N3.")]
    public IActionResult Submit(int id, PrescriptionSubmitRequest? request) => ToActionResult(service.SubmitPrescription(id, request));

    [HttpPut("{id:int}/cancel")]
    [EndpointSummary("Hủy đơn thuốc")]
    [EndpointDescription("Hủy đơn thuốc và lưu lý do hủy.")]
    public IActionResult Cancel(int id, PrescriptionCancelRequest request) => ToActionResult(service.CancelPrescription(id, request));
}
