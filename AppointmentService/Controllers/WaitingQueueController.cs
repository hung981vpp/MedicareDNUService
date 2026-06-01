using AppointmentService.Common;
using AppointmentService.Dtos.WaitingQueue;
using AppointmentService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Controllers;

[ApiController]
[Route("api/waiting-queue")]
[Tags("Waiting Queue")]
public sealed class WaitingQueueController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public WaitingQueueController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    /// <summary>
    /// Appointment Service API: list waiting queue entries.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<QueueEntryDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<QueueEntryDto>>> GetQueue([FromQuery] DateOnly? date)
    {
        var data = _appointmentService.GetWaitingQueue(date);
        return Ok(ApiResponse<IReadOnlyList<QueueEntryDto>>.Ok(data, "Waiting queue retrieved successfully"));
    }

    /// <summary>
    /// Appointment Service API: get one waiting queue entry by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<QueueEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QueueEntryDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<QueueEntryDto>> GetById(int id)
    {
        return ToActionResult(_appointmentService.GetQueueEntryById(id));
    }

    /// <summary>
    /// Appointment Service API: mark a waiting queue entry as in progress.
    /// </summary>
    [HttpPut("{id:int}/in-progress")]
    [ProducesResponseType(typeof(ApiResponse<QueueEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QueueEntryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<QueueEntryDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<QueueEntryDto>> Start(int id)
    {
        return ToActionResult(_appointmentService.StartQueueEntry(id));
    }

    /// <summary>
    /// Appointment Service API: mark a waiting queue entry as done.
    /// </summary>
    [HttpPut("{id:int}/done")]
    [ProducesResponseType(typeof(ApiResponse<QueueEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QueueEntryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<QueueEntryDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<QueueEntryDto>> Done(int id)
    {
        return ToActionResult(_appointmentService.CompleteQueueEntry(id));
    }

    /// <summary>
    /// Appointment Service API: cancel a waiting queue entry.
    /// </summary>
    [HttpPut("{id:int}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<QueueEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QueueEntryDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<QueueEntryDto>> Cancel(int id)
    {
        return ToActionResult(_appointmentService.CancelQueueEntry(id));
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
