using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Quản lý Dữ liệu Gốc (Master Data)
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
    /// [Module/Flow]: Quản lý Dữ liệu Gốc (Master Data)
    /// [Core Responsibility]: Lấy danh sách tất cả các khóa học.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllCourses(CancellationToken cancellationToken)
    {
        var courses = await _courseService.GetAllCoursesAsync(cancellationToken);
        return Ok(courses);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Dữ liệu Gốc (Master Data)
    /// [Core Responsibility]: Lấy thông tin một khóa học theo ID.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourse(int id, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetCourseByIdAsync(id, cancellationToken);
        return Ok(course);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Dữ liệu Gốc (Master Data)
    /// [Core Responsibility]: Tạo một khóa học mới.
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
    /// [Module/Flow]: Quản lý Dữ liệu Gốc (Master Data)
    /// [Core Responsibility]: Cập nhật một khóa học hiện có.
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
    /// [Module/Flow]: Quản lý Dữ liệu Gốc (Master Data)
    /// [Core Responsibility]: Xóa mềm (soft delete) một khóa học.
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


