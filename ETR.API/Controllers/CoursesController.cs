using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Master Data Management
/// [Core Responsibility]: Manages course catalogues.
/// [Target Audience]: Admin, Academic, TrainingManager
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ICurrentUserService _currentUserService;

    public CoursesController(ICourseService courseService, ICurrentUserService currentUserService)
    {
        _courseService = courseService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Retrieves all courses.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllCourses(CancellationToken cancellationToken)
    {
        var courses = await _courseService.GetAllCoursesAsync(cancellationToken);
        return Ok(courses);
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Retrieves a course by ID.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourse(int id, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetCourseByIdAsync(id, cancellationToken);
        return Ok(course);
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Creates a new course.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var course = await _courseService.CreateCourseAsync(request, accountId, cancellationToken);
        return CreatedAtAction(nameof(GetCourse), new { id = course.CourseId }, course);
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Updates an existing course.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var course = await _courseService.UpdateCourseAsync(id, request, accountId, cancellationToken);
        return Ok(course);
    }

    /// <summary>
    /// [Module/Flow]: Master Data Management
    /// [Core Responsibility]: Soft deletes a course.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCourse(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        await _courseService.DeleteCourseAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}


