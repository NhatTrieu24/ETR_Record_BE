using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly IUnitOfWork _unitOfWork;

    public EnrollmentsController(IEnrollmentService enrollmentService, IUnitOfWork unitOfWork)
    {
        _enrollmentService = enrollmentService;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnrollmentResponse>>> GetEnrollments(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.EnrollmentRepository.GetAllAsync(cancellationToken);
        var response = list.Select(e => new EnrollmentResponse(
            e.EnrollmentId,
            e.LearnerId,
            e.ClassId,
            e.Status,
            e.EnrolledAt));
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EnrollmentResponse>> GetEnrollmentById(int id, CancellationToken cancellationToken)
    {
        var enrollment = await _unitOfWork.EnrollmentRepository.GetByIdAsync(id, cancellationToken);
        if (enrollment == null) return NotFound($"Không tìm thấy bản ghi ghi danh với ID {id}.");
        
        var response = new EnrollmentResponse(
            enrollment.EnrollmentId,
            enrollment.LearnerId,
            enrollment.ClassId,
            enrollment.Status,
            enrollment.EnrolledAt);
            
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<CreateEnrollmentResponse>> Create(
        [FromBody] CreateEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _enrollmentService.CreateEnrollmentAsync(
            request.LearnerId,
            request.ClassId,
            request.CreatedByUserId,
            cancellationToken);

        return Created($"/api/enrollments/{result.EnrollmentId}", result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateEnrollment(int id, [FromBody] UpdateEnrollmentRequest request, CancellationToken cancellationToken)
    {
        if (id != request.EnrollmentId) return BadRequest("ID không khớp.");

        var existing = await _unitOfWork.EnrollmentRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy bản ghi ghi danh với ID {id}.");

        existing.LearnerId = request.LearnerId;
        existing.ClassId = request.ClassId;
        existing.Status = request.Status;
        existing.EnrolledAt = request.EnrolledAt;
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EnrollmentRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteEnrollment(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.EnrollmentRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy bản ghi ghi danh với ID {id}.");

        _unitOfWork.EnrollmentRepository.Delete(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<EnrollmentResponse>> CancelEnrollment(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.EnrollmentRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy bản ghi ghi danh với ID {id}.");

        existing.Status = "Cancelled";
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EnrollmentRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        var response = new EnrollmentResponse(
            existing.EnrollmentId,
            existing.LearnerId,
            existing.ClassId,
            existing.Status,
            existing.EnrolledAt);

        return Ok(response);
    }

    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<EnrollmentResponse>> ActivateEnrollment(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.EnrollmentRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy bản ghi ghi danh với ID {id}.");

        existing.Status = "Active";
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EnrollmentRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        var response = new EnrollmentResponse(
            existing.EnrollmentId,
            existing.LearnerId,
            existing.ClassId,
            existing.Status,
            existing.EnrolledAt);

        return Ok(response);
    }
}
