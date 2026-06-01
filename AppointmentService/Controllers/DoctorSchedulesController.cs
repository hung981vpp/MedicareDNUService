using AppointmentService.Common;
using AppointmentService.Dtos.DoctorSchedules;
using AppointmentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Controllers;

[ApiController]
[Route("api/doctor-schedules")]
[Tags("Doctor Schedules")]
public sealed class DoctorSchedulesController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public DoctorSchedulesController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    /// <summary>
    /// Appointment Service API: list configured doctor schedules.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DoctorScheduleDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<DoctorScheduleDto>>> GetSchedules()
    {
        var data = _appointmentService.GetDoctorSchedules();
        return Ok(ApiResponse<IReadOnlyList<DoctorScheduleDto>>.Ok(data, "Doctor schedules retrieved successfully"));
    }

    /// <summary>
    /// Appointment Service API: list schedules for one doctor.
    /// </summary>
    [HttpGet("doctor/{doctorId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DoctorScheduleDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<DoctorScheduleDto>>> GetSchedulesByDoctor(int doctorId)
    {
        var data = _appointmentService.GetDoctorSchedulesByDoctor(doctorId);
        return Ok(ApiResponse<IReadOnlyList<DoctorScheduleDto>>.Ok(data, "Doctor schedules by doctor retrieved successfully"));
    }

    /// <summary>
    /// Appointment Service API: get one doctor schedule by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DoctorScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DoctorScheduleDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<DoctorScheduleDto>> GetScheduleById(int id)
    {
        return ToActionResult(_appointmentService.GetDoctorScheduleById(id));
    }

    /// <summary>
    /// Appointment Service API: create a doctor schedule.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DoctorScheduleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<DoctorScheduleDto>), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<DoctorScheduleDto>> CreateSchedule(CreateDoctorScheduleRequest request)
    {
        var result = _appointmentService.CreateDoctorSchedule(request);
        if (!result.Success || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetScheduleById),
            new { id = result.Data.ScheduleId },
            ApiResponse<DoctorScheduleDto>.Ok(result.Data, result.Message));
    }

    /// <summary>
    /// Appointment Service API: update a doctor schedule.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<DoctorScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DoctorScheduleDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DoctorScheduleDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<DoctorScheduleDto>> UpdateSchedule(int id, UpdateDoctorScheduleRequest request)
    {
        return ToActionResult(_appointmentService.UpdateDoctorSchedule(id, request));
    }

    /// <summary>
    /// Appointment Service API: delete a doctor schedule when no active appointments use it.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<bool>> DeleteSchedule(int id)
    {
        return ToActionResult(_appointmentService.DeleteDoctorSchedule(id));
    }

    private ActionResult<ApiResponse<T>> ToActionResult<T>(ServiceResult<T> result)
    {
        if (result.Success && result.Data is not null)
        {
            return Ok(ApiResponse<T>.Ok(result.Data, result.Message));
        }

        var response = ApiResponse<T>.Fail(result.Message);
        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => NotFound(response),
            _ => BadRequest(response)
        };
    }
}
