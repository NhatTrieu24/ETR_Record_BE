using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Báo cáo &amp; Phân tích
/// [Core Responsibility]: Aggregates high-level statistics for system dashboards.
/// [Target Audience]: Admin, Management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// [Module/Flow]: Báo cáo &amp; Phân tích
    /// [Core Responsibility]: Lấy các số liệu thống kê tổng quan cho dashboard.
    /// [Target Audience]: Admin, Management
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var classes = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        return Ok(new { TotalClasses = classes.Count(), TotalEtrs = etrs.Count() });
    }
}


