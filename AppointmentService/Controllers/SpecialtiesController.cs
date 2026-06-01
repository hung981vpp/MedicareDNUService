using AppointmentService.Common;
using AppointmentService.Dtos.Specialties;
using AppointmentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Controllers;

[ApiController]
[Route("api/specialties")]
[Tags("Specialties")]
public sealed class SpecialtiesController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public SpecialtiesController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    /// <summary>
    /// Appointment Service API: list specialties owned by Appointment Service.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SpecialtyDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<SpecialtyDto>>> GetSpecialties()
    {
        var data = _appointmentService.GetSpecialties();
        return Ok(ApiResponse<IReadOnlyList<SpecialtyDto>>.Ok(data, "Specialties retrieved successfully"));
    }

    /// <summary>
    /// Appointment Service API: get one specialty by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SpecialtyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SpecialtyDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<SpecialtyDto>> GetSpecialtyById(int id)
    {
        return ToActionResult(_appointmentService.GetSpecialtyById(id));
    }

    /// <summary>
    /// Appointment Service API: create a specialty owned by Appointment Service.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SpecialtyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SpecialtyDto>), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<SpecialtyDto>> CreateSpecialty(CreateSpecialtyRequest request)
    {
        var result = _appointmentService.CreateSpecialty(request);
        if (!result.Success || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetSpecialtyById),
            new { id = result.Data.SpecialtyId },
            ApiResponse<SpecialtyDto>.Ok(result.Data, result.Message));
    }

    /// <summary>
    /// Appointment Service API: update a specialty owned by Appointment Service.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<SpecialtyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SpecialtyDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SpecialtyDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<SpecialtyDto>> UpdateSpecialty(int id, UpdateSpecialtyRequest request)
    {
        return ToActionResult(_appointmentService.UpdateSpecialty(id, request));
    }

    /// <summary>
    /// Appointment Service API: delete a specialty when no doctors reference it.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<bool>> DeleteSpecialty(int id)
    {
        return ToActionResult(_appointmentService.DeleteSpecialty(id));
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
