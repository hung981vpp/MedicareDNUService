using MedicalAPI.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAPI.Controllers;

[ApiController]
public abstract class MedicalControllerBase : ControllerBase
{
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
