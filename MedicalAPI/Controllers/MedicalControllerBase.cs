using MedicalAPI.Application.Common;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedicalAPI.Controllers;

[ApiController]
public abstract class MedicalControllerBase : ControllerBase
{
    protected bool IsPatient() => User.IsInRole("Patient");
    protected bool IsDoctor() => User.IsInRole("Doctor");
    protected bool IsAdmin() => User.IsInRole("Admin");
    protected bool IsNurse() => User.IsInRole("Nurse");
    protected bool IsReceptionist() => User.IsInRole("Receptionist");

    protected int? CurrentPatientId()
    {
        var value = User.FindFirst("PatientId")?.Value;
        return int.TryParse(value, out var patientId) ? patientId : null;
    }

    protected int? CurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return int.TryParse(value, out var userId) ? userId : null;
    }

    protected int? CurrentDoctorId()
    {
        var value = User.FindFirst("DoctorId")?.Value ?? User.FindFirst("doctorId")?.Value;
        return int.TryParse(value, out var doctorId) ? doctorId : null;
    }

    protected string? CurrentEmail()
        => User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;

    protected string? CurrentFullName()
        => User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("unique_name")?.Value;

    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        var traceId = Request.Headers.TryGetValue("X-Request-Id", out var requestId) && !string.IsNullOrWhiteSpace(requestId)
            ? requestId.ToString()
            : HttpContext.TraceIdentifier;

        var response = result.IsSuccess
            ? ApiResponse<T>.Ok(result.Data!, result.Message, traceId)
            : ApiResponse<T>.Fail(result.Message, traceId, result.Errors.ToArray());

        return StatusCode(result.StatusCode, response);
    }
}
