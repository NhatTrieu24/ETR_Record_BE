using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Document Management
/// [Core Responsibility]: Manages uploaded evidence files for practical checklists and assessments.
/// [Target Audience]: Instructor, Admin, Mentor
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EvidencesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public EvidencesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var files = await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken);
        return Ok(files);
    }
}
