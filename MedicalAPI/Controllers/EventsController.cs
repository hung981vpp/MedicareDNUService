using MedicalAPI.Application.DTOs;
using MedicalAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Lấy danh sách outbox event")]
    [EndpointDescription("Lấy các event do N2 tạo để gửi sang service khác, có thể lọc theo trạng thái và loại event.")]
    public IActionResult Outbox([FromQuery] string? status, [FromQuery] string? eventType)
        => ToActionResult(service.GetOutboxEvents(status, eventType));

    [HttpGet("inbox")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Lấy danh sách inbox event")]
    [EndpointDescription("Lấy các event N2 đã nhận/xử lý từ service khác, có thể lọc theo trạng thái và loại event.")]
    public IActionResult Inbox([FromQuery] string? status, [FromQuery] string? eventType)
        => ToActionResult(service.GetInboxEvents(status, eventType));

    [HttpPut("outbox/{id:int}/published")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Đánh dấu outbox đã gửi")]
    [EndpointDescription("Chuyển trạng thái outbox event sang Đã gửi sau khi publish thành công.")]
    public IActionResult MarkPublished(int id) => ToActionResult(service.MarkOutboxPublished(id));

    [HttpPut("outbox/{id:int}/retry")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Retry outbox event")]
    [EndpointDescription("Đưa outbox event về trạng thái chờ gửi để background worker publish lại.")]
    public IActionResult RetryOutbox(int id) => ToActionResult(service.RetryOutboxEvent(id));

    [HttpPut("outbox/{id:int}/fail")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Đánh dấu outbox thất bại")]
    [EndpointDescription("Đánh dấu event thất bại thủ công để frontend giám sát và xử lý.")]
    public IActionResult FailOutbox(int id) => ToActionResult(service.FailOutboxEvent(id));
}
