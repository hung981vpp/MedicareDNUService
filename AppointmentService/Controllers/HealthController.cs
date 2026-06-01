using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AppointmentService.Controllers;

[ApiController]
[Route("api/health")]
[Tags("Health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Appointment Service API: check service availability.
    /// </summary>
    [HttpGet]
    [HttpHead]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            service = "Appointment Service",
            status = "Healthy",
            timestamp = DateTime.UtcNow
        });
    }
}
