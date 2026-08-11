using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Bulk Import
/// [Core Responsibility]: Excel template generation and bulk data import for Attendance and Assessment scores.
/// [Target Audience]: Instructor (commit), Academic/Admin (all endpoints)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Instructor,Academic")]
public class ImportController : ControllerBase
{
    private readonly IImportService _importService;
    private readonly ICurrentUserService _currentUserService;

    public ImportController(IImportService importService, ICurrentUserService currentUserService)
    {
        _importService = importService;
        _currentUserService = currentUserService;
    }

    // ── Attendance ───────────────────────────────────────────────────────────

    /// <summary>
    /// [Module/Flow]: Bulk Import — Attendance
    /// [Core Responsibility]: Tải về file Excel mẫu điểm danh cho session, đã pre-fill danh sách học viên.
    /// [Target Audience]: Instructor, Academic, Admin
    /// </summary>
    [HttpGet("attendance/template")]
    public async Task<IActionResult> GetAttendanceTemplate([FromQuery] int sessionId, CancellationToken cancellationToken)
    {
        var bytes = await _importService.GenerateAttendanceTemplateAsync(sessionId, cancellationToken);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"attendance_session_{sessionId}.xlsx");
    }

    /// <summary>
    /// [Module/Flow]: Bulk Import — Attendance
    /// [Core Responsibility]: Validate file Excel điểm danh (dry-run, không ghi DB). Trả về danh sách lỗi nếu có.
    /// [Target Audience]: Instructor, Academic, Admin
    /// </summary>
    [HttpPost("attendance/validate")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ValidateAttendanceImport(
        [FromQuery] int sessionId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File không được để trống.");

        await using var stream = file.OpenReadStream();
        var result = await _importService.ValidateAttendanceImportAsync(sessionId, stream, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [Module/Flow]: Bulk Import — Attendance
    /// [Core Responsibility]: Validate và ghi toàn bộ điểm danh từ file Excel vào DB trong một transaction.
    /// [Target Audience]: Instructor, Academic, Admin
    /// </summary>
    [HttpPost("attendance/commit")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CommitAttendanceImport(
        [FromQuery] int sessionId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File không được để trống.");

        var accountId  = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var roleName   = _currentUserService.RoleName;

        await using var stream = file.OpenReadStream();
        var result = await _importService.CommitAttendanceImportAsync(sessionId, stream, accountId, roleName, cancellationToken);

        if (result.Errors.Count > 0 && result.Imported == 0)
            return BadRequest(result);

        return Ok(result);
    }

    // ── Assessment ───────────────────────────────────────────────────────────

    /// <summary>
    /// [Module/Flow]: Bulk Import — Assessment
    /// [Core Responsibility]: Tải về file Excel mẫu nhập điểm (lý thuyết/thực hành) đã pre-fill danh sách học viên.
    /// [Target Audience]: Instructor, Academic, Admin
    /// </summary>
    [HttpGet("assessment/template")]
    public async Task<IActionResult> GetAssessmentTemplate([FromQuery] int assessmentId, CancellationToken cancellationToken)
    {
        var bytes = await _importService.GenerateAssessmentTemplateAsync(assessmentId, cancellationToken);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"assessment_{assessmentId}.xlsx");
    }

    /// <summary>
    /// [Module/Flow]: Bulk Import — Assessment
    /// [Core Responsibility]: Validate file Excel nhập điểm (dry-run, không ghi DB). Trả về danh sách lỗi nếu có.
    /// [Target Audience]: Instructor, Academic, Admin
    /// </summary>
    [HttpPost("assessment/validate")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ValidateAssessmentImport(
        [FromQuery] int assessmentId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File không được để trống.");

        await using var stream = file.OpenReadStream();
        var result = await _importService.ValidateAssessmentImportAsync(assessmentId, stream, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [Module/Flow]: Bulk Import — Assessment
    /// [Core Responsibility]: Validate và ghi toàn bộ điểm từ file Excel vào DB trong một transaction.
    /// [Target Audience]: Instructor, Academic, Admin
    /// </summary>
    [HttpPost("assessment/commit")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CommitAssessmentImport(
        [FromQuery] int assessmentId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File không được để trống.");

        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var roleName  = _currentUserService.RoleName;

        await using var stream = file.OpenReadStream();
        var result = await _importService.CommitAssessmentImportAsync(assessmentId, stream, accountId, roleName, cancellationToken);

        if (result.Errors.Count > 0 && result.Imported == 0)
            return BadRequest(result);

        return Ok(result);
    }
}
