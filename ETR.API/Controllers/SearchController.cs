using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: System Discovery
/// [Core Responsibility]: Provides global search capabilities across classes and ETR records.
/// [Target Audience]: All Roles
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("classes")]
    public async Task<IActionResult> SearchClasses([FromQuery] string query, CancellationToken cancellationToken)
    {
        var classes = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var result = classes.Where(c => c.ClassName.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        return Ok(result);
    }

    [HttpGet("etrs")]
    public async Task<IActionResult> SearchEtrs([FromQuery] string query, CancellationToken cancellationToken)
    {
        var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        return Ok(etrs);
    }
}
