using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Master Data Management
/// [Core Responsibility]: Manages subject catalogues.
/// [Target Audience]: Admin, CROStaff
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SubjectsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public SubjectsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSubjects(CancellationToken cancellationToken)
    {
        var subjects = await _unitOfWork.SubjectRepository.GetAllAsync(cancellationToken);
        return Ok(subjects);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSubject(int id, CancellationToken cancellationToken)
    {
        var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(id, cancellationToken);
        if (subject == null) return NotFound();
        return Ok(subject);
    }
}
