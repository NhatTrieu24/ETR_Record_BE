using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Master Data Management
/// [Core Responsibility]: Manages course catalogues.
/// [Target Audience]: Admin, CROStaff
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CoursesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCourses(CancellationToken cancellationToken)
    {
        var courses = await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken);
        return Ok(courses);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourse(int id, CancellationToken cancellationToken)
    {
        var course = await _unitOfWork.CourseRepository.GetByIdAsync(id, cancellationToken);
        if (course == null) return NotFound();
        return Ok(course);
    }
}
