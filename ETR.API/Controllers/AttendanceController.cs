using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("record")]
    public async Task<IActionResult> RecordAttendance([FromBody] CreateAttendanceRecordRequest request, CancellationToken cancellationToken)
    {
        var response = await _attendanceService.RecordAttendanceAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("sessions/{sessionId}/confirm")]
    public async Task<IActionResult> ConfirmSession(int sessionId, [FromQuery] int confirmedByUserId, CancellationToken cancellationToken)
    {
        var response = await _attendanceService.ConfirmSessionAsync(sessionId, confirmedByUserId, cancellationToken);
        return Ok(response);
    }
}
