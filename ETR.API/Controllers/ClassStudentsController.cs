using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Academic,TrainingManager,Instructor,Audit")]
public class ClassStudentsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ClassStudentsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var classStudents = await _unitOfWork.ClassStudentRepository.GetAllAsync(cancellationToken);
        var result = classStudents.Where(cs => !cs.IsDeleted).Select(cs => new ClassStudentResponse(
            cs.ClassStudentId, cs.CourseEnrollmentId, cs.ClassId, cs.AccountId, cs.Status
        )).ToList();
        return Ok(result);
    }

    [HttpGet("class/{classId}")]
    public async Task<IActionResult> GetByClassId(int classId, CancellationToken cancellationToken)
    {
        var classStudents = await _unitOfWork.ClassStudentRepository.GetAllAsync(cancellationToken);
        var result = classStudents.Where(cs => cs.ClassId == classId && !cs.IsDeleted).Select(cs => new ClassStudentResponse(
            cs.ClassStudentId, cs.CourseEnrollmentId, cs.ClassId, cs.AccountId, cs.Status
        )).ToList();
        return Ok(result);
    }

    [HttpGet("enrollment/{enrollmentId}")]
    public async Task<IActionResult> GetByEnrollmentId(int enrollmentId, CancellationToken cancellationToken)
    {
        var classStudents = await _unitOfWork.ClassStudentRepository.GetAllAsync(cancellationToken);
        var result = classStudents.Where(cs => cs.CourseEnrollmentId == enrollmentId && !cs.IsDeleted).Select(cs => new ClassStudentResponse(
            cs.ClassStudentId, cs.CourseEnrollmentId, cs.ClassId, cs.AccountId, cs.Status
        )).FirstOrDefault();

        if (result == null) return NotFound("ClassStudent not found for the given enrollment.");
        return Ok(result);
    }
}
