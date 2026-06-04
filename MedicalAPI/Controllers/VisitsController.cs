using MedicalAPI.Application.DTOs;
using MedicalAPI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAPI.Controllers;

[Route("api/v1/medical/visits")]
public sealed class VisitsController(IMedicalRecordService service) : MedicalControllerBase
{
    [HttpGet("today")]
    [EndpointSummary("Danh sách lượt khám hôm nay")]
    [EndpointDescription("Lấy hàng chờ khám của ngày hiện tại, có thể lọc theo bác sĩ.")]
    public IActionResult GetToday([FromQuery] int? doctorId) => ToActionResult(service.GetTodayVisits(doctorId));

    [HttpGet("{id:int}")]
    [EndpointSummary("Xem chi tiết lượt khám")]
    [EndpointDescription("Lấy thông tin lượt khám, bệnh nhân, bác sĩ, sinh hiệu và trạng thái.")]
    public IActionResult GetById(int id) => ToActionResult(service.GetVisit(id));

    [HttpGet("by-appointment/{appointmentId:int}")]
    [EndpointSummary("Lấy lượt khám theo lịch hẹn")]
    [EndpointDescription("Dùng cho frontend kiểm tra lịch hẹn N1 đã được N2 tạo lượt khám hay chưa.")]
    public IActionResult GetByAppointment(int appointmentId) => ToActionResult(service.GetVisitByAppointment(appointmentId));

    [HttpPost]
    [EndpointSummary("Tạo lượt khám")]
    [EndpointDescription("Tạo lượt khám thủ công khi cần bổ sung cho một lịch hẹn hoặc bệnh nhân khám trực tiếp.")]
    public IActionResult Create(VisitCreateRequest request) => ToActionResult(service.CreateVisit(request));

    [HttpPut("{id:int}/start")]
    [EndpointSummary("Bắt đầu khám")]
    [EndpointDescription("Chuyển lượt khám sang trạng thái Đang khám và ghi nhận lý do khám.")]
    public IActionResult Start(int id, VisitStartRequest request) => ToActionResult(service.StartVisit(id, request));

    [HttpPut("{id:int}/vitals")]
    [EndpointSummary("Cập nhật sinh hiệu")]
    [EndpointDescription("Ghi nhận nhiệt độ, huyết áp, nhịp tim, chiều cao, cân nặng và ghi chú điều dưỡng.")]
    public IActionResult UpdateVitals(int id, VisitVitalsRequest request) => ToActionResult(service.UpdateVitals(id, request));

    [HttpPut("{id:int}/complete")]
    [EndpointSummary("Hoàn tất lượt khám")]
    [EndpointDescription("Hoàn tất lượt khám sau khi bệnh án đã được hoàn tất.")]
    public IActionResult Complete(int id) => ToActionResult(service.CompleteVisit(id));

    [HttpPut("{id:int}/cancel")]
    [EndpointSummary("Hủy lượt khám")]
    [EndpointDescription("Hủy lượt khám và lưu lý do hủy.")]
    public IActionResult Cancel(int id, VisitCancelRequest request) => ToActionResult(service.CancelVisit(id, request));
}
