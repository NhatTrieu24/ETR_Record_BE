using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Master Data Management
/// [Core Responsibility]: Manages subject catalogues.
/// [Target Audience]: Admin, Academic, TrainingManager
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Academic,TrainingManager")]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;
    private readonly ICurrentUserService _currentUserService;

    public SubjectsController(ISubjectService subjectService, ICurrentUserService currentUserService)
    {
        _subjectService = subjectService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Retrieves all subjects.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllSubjects(CancellationToken cancellationToken)
    {
        var subjects = await _subjectService.GetAllSubjectsAsync(cancellationToken);
        return Ok(subjects);
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Retrieves a subject by ID.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSubject(int id, CancellationToken cancellationToken)
    {
        var subject = await _subjectService.GetSubjectByIdAsync(id, cancellationToken);
        return Ok(subject);
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Creates a new subject.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var subject = await _subjectService.CreateSubjectAsync(request, accountId, cancellationToken);
        return CreatedAtAction(nameof(GetSubject), new { id = subject.SubjectId }, subject);
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Updates an existing subject.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSubject(int id, [FromBody] UpdateSubjectRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var subject = await _subjectService.UpdateSubjectAsync(id, request, accountId, cancellationToken);
        return Ok(subject);
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Soft deletes a subject.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubject(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        await _subjectService.DeleteSubjectAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}
