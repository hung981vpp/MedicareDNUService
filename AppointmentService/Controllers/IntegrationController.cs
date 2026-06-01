using AppointmentService.Common;
using AppointmentService.Dtos.Appointments;
using AppointmentService.Dtos.Integration;
using AppointmentService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Controllers;

[ApiController]
[Route("api/integration/appointments")]
[Tags("Service Integration - Appointments")]
public sealed class IntegrationController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public IntegrationController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    /// <summary>
    /// N2 Medical Record Service: get confirmed appointment data before creating a medical record.
    /// </summary>
    [HttpGet("{appointmentId:int}/medical-info")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentForMedicalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentForMedicalDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentForMedicalDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<AppointmentForMedicalDto>> GetMedicalInfo(int appointmentId)
    {
        return ToActionResult(_appointmentService.GetMedicalInfo(appointmentId));
    }

    /// <summary>
    /// N3 Pharmacy &amp; Billing Service: get confirmed/completed appointment billing data for invoice creation.
    /// </summary>
    [HttpGet("{appointmentId:int}/billing-info")]
    [ProducesResponseType(typeof(ApiResponse<BillingInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BillingInfoDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BillingInfoDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<BillingInfoDto>> GetBillingInfo(int appointmentId)
    {
        return ToActionResult(_appointmentService.GetBillingInfo(appointmentId));
    }

    /// <summary>
    /// Service integration: get appointments by patient id without owning patient records.
    /// </summary>
    [HttpGet("patient/{patientId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppointmentDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<AppointmentDto>>> GetByPatient(int patientId)
    {
        var data = _appointmentService.GetAppointmentsByPatient(patientId);
        return Ok(ApiResponse<IReadOnlyList<AppointmentDto>>.Ok(data, "Patient appointments retrieved successfully"));
    }

    /// <summary>
    /// Service integration: get appointments by doctor id.
    /// </summary>
    [HttpGet("doctor/{doctorId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppointmentDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<AppointmentDto>>> GetByDoctor(int doctorId)
    {
        var data = _appointmentService.GetAppointmentsByDoctor(doctorId);
        return Ok(ApiResponse<IReadOnlyList<AppointmentDto>>.Ok(data, "Doctor appointments retrieved successfully"));
    }

    /// <summary>
    /// Service integration: inspect in-memory integration events prepared for a future message broker.
    /// </summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppointmentEventDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<AppointmentEventDto>>> GetEvents()
    {
        var data = _appointmentService.GetIntegrationEvents();
        return Ok(ApiResponse<IReadOnlyList<AppointmentEventDto>>.Ok(data, "Appointment integration events retrieved successfully"));
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
