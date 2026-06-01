using MedicalAPI.Application.DTOs;
using MedicalAPI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAPI.Controllers;

[Route("api/v1/medical/events")]
public sealed class EventsController(IMedicalRecordService service) : MedicalControllerBase
{
    [HttpPost("appointment-confirmed")]
    [EndpointSummary("Nhận event appointment.confirmed")]
    [EndpointDescription("Xử lý event xác nhận lịch hẹn từ N1, tạo hoặc cập nhật bệnh nhân và lưu snapshot lịch hẹn.")]
    public IActionResult AppointmentConfirmed(AppointmentConfirmedEventRequest request)
        => ToActionResult(service.HandleAppointmentConfirmed(request));

    [HttpPost("patient-checked-in")]
    [EndpointSummary("Nhận event patient.checked_in")]
    [EndpointDescription("Xử lý event bệnh nhân đã đến khám từ N1 và tạo lượt khám trạng thái Chờ khám.")]
    public IActionResult PatientCheckedIn(PatientCheckedInEventRequest request)
        => ToActionResult(service.HandlePatientCheckedIn(request));

    [HttpGet("outbox")]
    [EndpointSummary("Lấy danh sách outbox event")]
    [EndpointDescription("Lấy các event do N2 tạo để gửi sang service khác, có thể lọc theo trạng thái.")]
    public IActionResult Outbox([FromQuery] string? status) => ToActionResult(service.GetOutboxEvents(status));

    [HttpPut("outbox/{id:int}/published")]
    [EndpointSummary("Đánh dấu outbox đã gửi")]
    [EndpointDescription("Chuyển trạng thái outbox event sang Đã gửi sau khi publish thành công.")]
    public IActionResult MarkPublished(int id) => ToActionResult(service.MarkOutboxPublished(id));
}
