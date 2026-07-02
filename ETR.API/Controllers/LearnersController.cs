using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/learners")]
public class LearnersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILearnerService _learnerService;

    public LearnersController(IUnitOfWork unitOfWork, ILearnerService learnerService)
    {
        _unitOfWork = unitOfWork;
        _learnerService = learnerService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LearnerResponse>>> GetLearners(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.LearnerRepository.GetAllAsync(cancellationToken);
        var response = list.Select(MapToResponse);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LearnerResponse>> GetLearnerById(int id, CancellationToken cancellationToken)
    {
        var learner = await _unitOfWork.LearnerRepository.GetByIdAsync(id, cancellationToken);
        if (learner == null) return NotFound($"Không tìm thấy học viên với ID {id}.");
        return Ok(MapToResponse(learner));
    }

    [HttpPost]
    public async Task<ActionResult<LearnerResponse>> CreateLearner([FromBody] CreateLearnerRequest request, CancellationToken cancellationToken)
    {
        var learner = await _learnerService.CreateLearnerAsync(
            request.LearnerCode,
            request.FullName,
            request.IdentificationNumber,
            request.LearnerTypeId,
            cancellationToken);

        return CreatedAtAction(nameof(GetLearnerById), new { id = learner.LearnerId }, learner);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateLearner(int id, [FromBody] UpdateLearnerRequest request, CancellationToken cancellationToken)
    {
        if (id != request.LearnerId) return BadRequest("ID không khớp.");

        var existing = await _unitOfWork.LearnerRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy học viên với ID {id}.");

        existing.LearnerCode = request.LearnerCode;
        existing.FullName = request.FullName;
        existing.DateOfBirth = request.DateOfBirth;
        existing.Gender = request.Gender;
        existing.Phone = request.Phone;
        existing.Email = request.Email;
        existing.IdentificationNumber = request.IdentificationNumber;
        existing.Organization = request.Organization;
        existing.Status = request.Status;
        existing.LearnerTypeId = request.LearnerTypeId;
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.LearnerRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteLearner(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.LearnerRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy học viên với ID {id}.");

        _unitOfWork.LearnerRepository.Delete(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<LearnerResponse>>> SearchLearners([FromQuery] string query, CancellationToken cancellationToken)
    {
        var learners = await _unitOfWork.LearnerRepository.GetAllAsync(cancellationToken);
        var filtered = learners.Where(l => l.LearnerCode.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                                           l.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                           (l.Email != null && l.Email.Contains(query, StringComparison.OrdinalIgnoreCase)));
        return Ok(filtered.Select(MapToResponse));
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IEnumerable<EnrollmentResponse>>> GetLearnerHistory(int id, CancellationToken cancellationToken)
    {
        var enrollments = await _unitOfWork.EnrollmentRepository.GetAllAsync(cancellationToken);
        var filtered = enrollments.Where(e => e.LearnerId == id);
        var response = filtered.Select(e => new EnrollmentResponse(
            e.EnrollmentId,
            e.LearnerId,
            e.ClassId,
            e.Status,
            e.EnrolledAt));
        return Ok(response);
    }

    [HttpGet("{id:int}/etr")]
    public async Task<ActionResult<IEnumerable<EtrRecordResponse>>> GetLearnerEtr(int id, CancellationToken cancellationToken)
    {
        var enrollments = (await _unitOfWork.EnrollmentRepository.GetAllAsync(cancellationToken))
            .Where(e => e.LearnerId == id)
            .Select(e => e.EnrollmentId)
            .ToList();

        var etrs = (await _unitOfWork.ETRRecordRepository.GetAllAsync(cancellationToken))
            .Where(e => enrollments.Contains(e.EnrollmentId));

        var response = etrs.Select(e => new EtrRecordResponse(
            e.ETRRecordId,
            e.EnrollmentId,
            e.Status,
            e.IsLocked,
            e.SubmittedAt,
            e.VerifiedAt,
            e.CompletedAt));

        return Ok(response);
    }

    private static LearnerResponse MapToResponse(Learner l)
    {
        return new LearnerResponse(
            l.LearnerId,
            l.LearnerCode,
            l.FullName,
            l.DateOfBirth,
            l.Gender,
            l.Phone,
            l.Email,
            l.IdentificationNumber,
            l.Organization,
            l.Status,
            l.LearnerTypeId);
    }
}
