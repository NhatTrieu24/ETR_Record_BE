using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Reporting &amp; Analytics
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
    /// [Module/Flow]: Reporting &amp; Analytics
    /// [Core Responsibility]: Retrieves summary reports for classes and ETRs.
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
