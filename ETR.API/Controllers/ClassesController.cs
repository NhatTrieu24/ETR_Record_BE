using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Master Data Management
/// [Core Responsibility]: Manages classes and scheduling.
/// [Target Audience]: Admin, Academic, TrainingManager
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Academic,TrainingManager")]
public class ClassesController : ControllerBase
{
    private readonly IClassService _classService;
    private readonly ICurrentUserService _currentUserService;

    public ClassesController(IClassService classService, ICurrentUserService currentUserService)
    {
        _classService = classService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Retrieves all classes.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllClasses(CancellationToken cancellationToken)
    {
        var classes = await _classService.GetAllClassesAsync(cancellationToken);
        return Ok(classes);
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Retrieves a class by ID.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClass(int id, CancellationToken cancellationToken)
    {
        var cls = await _classService.GetClassByIdAsync(id, cancellationToken);
        return Ok(cls);
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Creates a new class.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var cls = await _classService.CreateClassAsync(request, accountId, cancellationToken);
        return CreatedAtAction(nameof(GetClass), new { id = cls.ClassId }, cls);
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Updates an existing class.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClass(int id, [FromBody] UpdateClassRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var cls = await _classService.UpdateClassAsync(id, request, accountId, cancellationToken);
        return Ok(cls);
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Soft deletes a class.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClass(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        await _classService.DeleteClassAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}
