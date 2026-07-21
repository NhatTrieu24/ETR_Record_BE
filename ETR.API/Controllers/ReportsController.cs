using ETR.Application.Interfaces;
using ETR.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Báo cáo &amp; Phân tích
/// [Core Responsibility]: Generates summary reports for ETR and Class data.
/// [Target Audience]: Admin, Management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,TrainingManager")]
public class ReportsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// [Module/Flow]: Báo cáo &amp; Phân tích
    /// [Core Responsibility]: Lấy các báo cáo tổng hợp cho các lớp học và ETR.
    /// [Target Audience]: Admin, Management
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var classes = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var kpis = await DashboardKpiCalculator.ComputeAsync(_unitOfWork, cancellationToken);
        return Ok(new { TotalClasses = classes.Count(), TotalEtrs = kpis.TotalEtrs, kpis.CompletedCount, kpis.CompletionRatePercent, kpis.PendingApprovalCount, kpis.RejectedCount, kpis.ReturnedForCorrectionCount, kpis.MissingEvidenceCount });
    }
}


