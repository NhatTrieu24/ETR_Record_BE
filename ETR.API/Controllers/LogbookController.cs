using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Sổ Bay &amp; Tổng Hợp Giờ Huấn Luyện (Pilot Logbook &amp; Hours Summary)
/// [Core Responsibility]: Cung cấp dữ liệu tổng hợp giờ bay, giờ SIM và lịch sử chi tiết từ các buổi học đã ký nhận.
/// [Target Audience]: Học viên (xem của mình), Giảng viên (lớp được phân công), Ban Đào tạo, Admin, QA, Audit
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LogbookController : ControllerBase
{
    private readonly ILogbookService _logbookService;
    private readonly ICurrentUserService _currentUserService;

    public LogbookController(ILogbookService logbookService, ICurrentUserService currentUserService)
    {
        _logbookService = logbookService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// [Module/Flow]: Sổ Bay &amp; Tổng Hợp Giờ Huấn Luyện
    /// [Core Responsibility]: Lấy dữ liệu tổng hợp sổ bay (Logbook Summary) của chính học viên đang đăng nhập.
    /// [Target Audience]: All Roles (Student, Learner)
    /// </summary>
    [HttpGet("my-summary")]
    public async Task<ActionResult<LogbookSummaryResponse>> GetMyLogbookSummary(CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var summary = await _logbookService.GetStudentLogbookSummaryAsync(
            accountId,
            accountId,
            _currentUserService.RoleName ?? string.Empty,
            cancellationToken);

        return Ok(summary);
    }

    /// <summary>
    /// [Module/Flow]: Sổ Bay &amp; Tổng Hợp Giờ Huấn Luyện
    /// [Core Responsibility]: Lấy dữ liệu tổng hợp sổ bay (Logbook Summary) của một học viên cụ thể theo Account ID.
    /// [Target Audience]: Admin, Academic, TrainingManager, QA, Audit, Instructor
    /// </summary>
    [HttpGet("student/{accountId:int}")]
    [Authorize(Roles = "Admin,Academic,TrainingManager,QA,Audit,Instructor")]
    public async Task<ActionResult<LogbookSummaryResponse>> GetStudentLogbookSummary(
        int accountId,
        CancellationToken cancellationToken)
    {
        var currentAccountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var summary = await _logbookService.GetStudentLogbookSummaryAsync(
            accountId,
            currentAccountId,
            _currentUserService.RoleName ?? string.Empty,
            cancellationToken);

        return Ok(summary);
    }
}
