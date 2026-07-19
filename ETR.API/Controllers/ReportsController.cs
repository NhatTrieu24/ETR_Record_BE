using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Báo cáo &amp; Phân tích
/// [Core Responsibility]: Generates summary reports for ETR and Class data.
/// [Target Audience]: Admin, Management
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
        var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        return Ok(new { TotalClasses = classes.Count(), TotalEtrs = etrs.Count() });
    }
}


