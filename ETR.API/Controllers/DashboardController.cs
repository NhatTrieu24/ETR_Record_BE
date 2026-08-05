using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Báo cáo &amp; Phân tích
/// [Core Responsibility]: Aggregates high-level statistics for system dashboards.
/// [Target Audience]: All Roles (each action narrows further — see per-action role list)
/// </summary>
// NOTE: class-level is authentication-only, not role-restricted — see EtrController.cs for why a
// class-level Roles list here would silently void method-level roles this controller's actions
// grant but the class-level list omits (ASP.NET Core combines class+method [Authorize] via AND).
// GetMyDashboard needs Instructor/QA/Student in addition to stats/action-items' original 5 roles,
// so each action now owns its own role list instead of inheriting a shared one.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDashboardService _dashboardService;

    public DashboardController(IUnitOfWork unitOfWork, IDashboardService dashboardService)
    {
        _unitOfWork = unitOfWork;
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// [Module/Flow]: Báo cáo &amp; Phân tích
    /// [Core Responsibility]: Lấy các số liệu thống kê tổng quan cho dashboard.
    /// [Target Audience]: Admin, Management
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin,TrainingManager,Audit,Academic,ManagementViewer")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var classes = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var kpis = await DashboardKpiCalculator.ComputeAsync(_unitOfWork, cancellationToken);
        return Ok(new { TotalClasses = classes.Count(), TotalEtrs = kpis.TotalEtrs, kpis.CompletedCount, kpis.CompletionRatePercent, kpis.PendingApprovalCount, kpis.RejectedCount, kpis.ReturnedForCorrectionCount, kpis.MissingEvidenceCount });
    }

    /// <summary>
    /// [Module/Flow]: Báo cáo &amp; Phân tích
    /// [Core Responsibility]: Lấy danh sách ID hồ sơ ETR theo từng nhóm hành động cần xử lý (đôn đốc),
    /// thay vì chỉ trả số đếm như `stats`.
    /// [Target Audience]: Admin, Management
    /// </summary>
    [HttpGet("action-items")]
    [Authorize(Roles = "Admin,TrainingManager,Audit,Academic,ManagementViewer")]
    public async Task<IActionResult> GetActionItems(CancellationToken cancellationToken)
    {
        var actionItems = await DashboardKpiCalculator.ComputeActionItemsAsync(_unitOfWork, cancellationToken);
        return Ok(actionItems);
    }

    /// <summary>
    /// [Module/Flow]: Báo cáo &amp; Phân tích
    /// [Core Responsibility]: Lấy dashboard phù hợp với vai trò của người dùng hiện tại — mỗi role
    /// chỉ nhận đúng widget của mình (Admin/TrainingManager/Academic/ManagementViewer/Audit: overview +
    /// status funnel + action items; Instructor: lớp mình dạy + học viên điểm danh thấp; QA: ETR chờ
    /// verify; Student: ETR của chính mình kèm % hoàn thành).
    /// [Target Audience]: All Roles
    /// </summary>
    [HttpGet("my-dashboard")]
    public async Task<ActionResult<MyDashboardResponse>> GetMyDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardService.GetMyDashboardAsync(cancellationToken);
        return Ok(dashboard);
    }
}


