using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Thực thi Đào tạo
/// [Core Responsibility]: Records student attendance and confirms session completion.
/// [Target Audience]: Instructor, Admin
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    /// [Module/Flow]: Thực thi Đào tạo
    /// [Core Responsibility]: Điểm danh cho một buổi học (session) cụ thể.
    /// [Target Audience]: Instructor, Admin
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
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _attendanceService.RecordAttendanceAsync(request, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// [Module/Flow]: Thực thi Đào tạo
    /// [Core Responsibility]: Xác nhận phiên điểm danh đã được chốt (finalized).
    /// [Target Audience]: Instructor, Admin
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
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _attendanceService.ConfirmSessionAsync(sessionId, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// [Module/Flow]: Thực thi Đào tạo
    /// [Core Responsibility]: Lấy lịch sử điểm danh của một học viên cụ thể trong lớp.
    /// [Target Audience]: Instructor, Admin, Student
    /// </summary>
    /// <param name="classStudentId">The ClassStudent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of attendance records.</returns>
    /// <response code="200">Returns the list of attendance records.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the ClassStudent is not found.</response>
    [HttpGet("student/{classStudentId}")]
    [Authorize] // Allow students to also fetch their own results, validation inside service
    public async Task<IActionResult> GetAttendanceRecords(int classStudentId, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _attendanceService.GetAttendanceByClassStudentAsync(classStudentId, accountId, cancellationToken);
        return Ok(response);
    }
}


