using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Quản lý Dữ liệu Gốc (Master Data)
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
    /// [Module/Flow]: Quản lý Dữ liệu Gốc (Master Data)
    /// [Core Responsibility]: Lấy danh sách tất cả các môn học.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllSubjects(CancellationToken cancellationToken)
    {
        var subjects = await _subjectService.GetAllSubjectsAsync(cancellationToken);
        return Ok(subjects);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Dữ liệu Gốc (Master Data)
    /// [Core Responsibility]: Lấy thông tin một môn học theo ID.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSubject(int id, CancellationToken cancellationToken)
    {
        var subject = await _subjectService.GetSubjectByIdAsync(id, cancellationToken);
        return Ok(subject);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Dữ liệu Gốc (Master Data)
    /// [Core Responsibility]: Tạo một môn học mới.
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
    /// [Module/Flow]: Quản lý Dữ liệu Gốc (Master Data)
    /// [Core Responsibility]: Cập nhật một môn học hiện có.
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
    /// [Module/Flow]: Quản lý Dữ liệu Gốc (Master Data)
    /// [Core Responsibility]: Xóa mềm (soft delete) một môn học.
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


