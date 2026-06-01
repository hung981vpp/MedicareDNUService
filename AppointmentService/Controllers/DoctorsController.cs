using AppointmentService.Common;
using AppointmentService.Dtos.Doctors;
using AppointmentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Controllers;

[ApiController]
[Route("api/doctors")]
[Tags("Doctors")]
public sealed class DoctorsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public DoctorsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    /// <summary>
    /// Appointment Service API: list doctors owned by Appointment Service.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DoctorDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<DoctorDto>>> GetDoctors()
    {
        var data = _appointmentService.GetDoctors();
        return Ok(ApiResponse<IReadOnlyList<DoctorDto>>.Ok(data, "Doctors retrieved successfully"));
    }

    /// <summary>
    /// Appointment Service API: list doctors by specialty.
    /// </summary>
    [HttpGet("by-specialty/{specialtyId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DoctorDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<DoctorDto>>> GetDoctorsBySpecialty(int specialtyId)
    {
        var data = _appointmentService.GetDoctorsBySpecialty(specialtyId);
        return Ok(ApiResponse<IReadOnlyList<DoctorDto>>.Ok(data, "Doctors by specialty retrieved successfully"));
    }

    /// <summary>
    /// Appointment Service API: get available appointment slots for a doctor and date.
    /// </summary>
    [HttpGet("{doctorId:int}/available-slots")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TimeOnly>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TimeOnly>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TimeOnly>>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<IReadOnlyList<TimeOnly>>> GetAvailableSlots(int doctorId, [FromQuery] DateOnly date)
    {
        return ToActionResult(_appointmentService.GetAvailableSlots(doctorId, date));
    }

    /// <summary>
    /// Appointment Service API: get one doctor by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<DoctorDto>> GetDoctorById(int id)
    {
        return ToActionResult(_appointmentService.GetDoctorById(id));
    }

    /// <summary>
    /// Appointment Service API: create a doctor owned by Appointment Service.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<DoctorDto>> CreateDoctor(CreateDoctorRequest request)
    {
        var result = _appointmentService.CreateDoctor(request);
        if (!result.Success || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetDoctorById),
            new { id = result.Data.DoctorId },
            ApiResponse<DoctorDto>.Ok(result.Data, result.Message));
    }

    /// <summary>
    /// Appointment Service API: update a doctor owned by Appointment Service.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<DoctorDto>> UpdateDoctor(int id, UpdateDoctorRequest request)
    {
        return ToActionResult(_appointmentService.UpdateDoctor(id, request));
    }

    /// <summary>
    /// Appointment Service API: delete a doctor when no appointments or schedules reference it.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<bool>> DeleteDoctor(int id)
    {
        return ToActionResult(_appointmentService.DeleteDoctor(id));
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
