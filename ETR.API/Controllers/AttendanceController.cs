using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// Handles attendance recording and session confirmation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Instructor, Admin")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ICurrentUserService _currentUserService;

    public AttendanceController(IAttendanceService attendanceService, ICurrentUserService currentUserService)
    {
        _attendanceService = attendanceService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Records attendance for a specific session.
    /// </summary>
    /// <param name="request">The attendance record details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recorded attendance response.</returns>
    /// <response code="200">Returns the recorded attendance.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not an Instructor or Admin.</response>
    [HttpPost("record")]
    public async Task<IActionResult> RecordAttendance([FromBody] CreateAttendanceRecordRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _attendanceService.RecordAttendanceAsync(request, userId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Confirms that an attendance session has been finalized.
    /// </summary>
    /// <param name="sessionId">The ID of the session to confirm.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The confirmed session response.</returns>
    /// <response code="200">Returns the confirmed session details.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not an Instructor or Admin.</response>
    [HttpPost("sessions/{sessionId}/confirm")]
    public async Task<IActionResult> ConfirmSession(int sessionId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _attendanceService.ConfirmSessionAsync(sessionId, userId, cancellationToken);
        return Ok(response);
    }
}
