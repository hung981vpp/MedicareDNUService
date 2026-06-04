using AppointmentService.Common;
using AppointmentService.Dtos.Appointments;
using AppointmentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Controllers;

[ApiController]
[Route("api/appointments")]
[Tags("Appointments")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    /// <summary>
    /// Appointment Service API: list all appointments.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppointmentDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<AppointmentDto>>> GetAll()
    {
        var data = _appointmentService.GetAppointments();
        return Ok(ApiResponse<IReadOnlyList<AppointmentDto>>.Ok(data, "Appointments retrieved successfully"));
    }

    /// <summary>
    /// Appointment Service API: get one appointment by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<AppointmentDto>> GetById(int id)
    {
        return ToActionResult(_appointmentService.GetAppointmentById(id));
    }

    /// <summary>
    /// Appointment Service API: get appointments by Medical Record patient id.
    /// </summary>
    [HttpGet("patient/{patientId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppointmentDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<AppointmentDto>>> GetByPatient(int patientId)
    {
        var data = _appointmentService.GetAppointmentsByPatient(patientId);
        return Ok(ApiResponse<IReadOnlyList<AppointmentDto>>.Ok(data, "Patient appointments retrieved successfully"));
    }

    /// <summary>
    /// Appointment Service API: get appointments by doctor id.
    /// </summary>
    [HttpGet("doctor/{doctorId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppointmentDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<AppointmentDto>>> GetByDoctor(int doctorId)
    {
        var data = _appointmentService.GetAppointmentsByDoctor(doctorId);
        return Ok(ApiResponse<IReadOnlyList<AppointmentDto>>.Ok(data, "Doctor appointments retrieved successfully"));
    }

    /// <summary>
    /// Appointment Service API: get confirmed, in-progress, and completed appointments.
    /// </summary>
    [HttpGet("confirmed")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppointmentDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<AppointmentDto>>> GetConfirmed()
    {
        var data = _appointmentService.GetConfirmedAppointments();
        return Ok(ApiResponse<IReadOnlyList<AppointmentDto>>.Ok(data, "Confirmed appointments retrieved successfully"));
    }

    /// <summary>
    /// Appointment Service API: create a new appointment using patient snapshot data from N2.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<AppointmentDto>> Create(CreateAppointmentRequest request)
    {
        var result = _appointmentService.CreateAppointment(request);
        if (!result.Success || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Data.AppointmentId },
            ApiResponse<AppointmentDto>.Ok(result.Data, result.Message));
    }

    /// <summary>
    /// Appointment Service API: confirm a pending appointment.
    /// </summary>
    [HttpPut("{id:int}/confirm")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<AppointmentDto>> Confirm(int id)
    {
        return ToActionResult(_appointmentService.ConfirmAppointment(id));
    }

    /// <summary>
    /// Appointment Service API: mark a confirmed appointment as in progress.
    /// </summary>
    [HttpPut("{id:int}/in-progress")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<AppointmentDto>> Start(int id)
    {
        return ToActionResult(_appointmentService.StartAppointment(id));
    }

    /// <summary>
    /// Appointment Service API: cancel an appointment.
    /// </summary>
    [HttpPut("{id:int}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<AppointmentDto>> Cancel(int id)
    {
        return ToActionResult(_appointmentService.CancelAppointment(id));
    }

    /// <summary>
    /// Appointment Service API: mark an in-progress appointment as completed.
    /// </summary>
    [HttpPut("{id:int}/complete")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<AppointmentDto>> Complete(int id)
    {
        return ToActionResult(_appointmentService.CompleteAppointment(id));
    }

    /// <summary>
    /// Appointment Service API: check in a patient for a confirmed appointment.
    /// </summary>
    [HttpPut("{id:int}/check-in")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<AppointmentDto>> CheckIn(int id)
    {
        return ToActionResult(_appointmentService.CheckInAppointment(id));
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
