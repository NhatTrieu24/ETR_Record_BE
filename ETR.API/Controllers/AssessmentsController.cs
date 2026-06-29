using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api")]
public class AssessmentsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AssessmentsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("assessment-results")]
    public async Task<ActionResult<IEnumerable<AssessmentResultResponse>>> GetResults(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.AssessmentResultRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapResultToResponse));
    }

    [HttpGet("assessment-results/{id:int}")]
    public async Task<ActionResult<AssessmentResultResponse>> GetResultById(int id, CancellationToken cancellationToken)
    {
        var res = await _unitOfWork.AssessmentResultRepository.GetByIdAsync(id, cancellationToken);
        if (res == null) return NotFound($"Không tìm thấy kết quả đánh giá với ID {id}.");
        return Ok(MapResultToResponse(res));
    }

    [HttpPost("assessment-results")]
    public async Task<ActionResult<AssessmentResultResponse>> CreateResult([FromBody] CreateAssessmentResultRequest request, CancellationToken cancellationToken)
    {
        var res = new AssessmentResult
        {
            AssessmentComponentId = request.AssessmentComponentId,
            LearnerId = request.LearnerId,
            ETRRecordId = request.ETRRecordId,
            Score = request.Score,
            ResultStatus = "Pending",
            InstructorComment = request.InstructorComment,
            RecordedBy = request.RecordedBy,
            RecordedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
            IsPublished = false
        };

        await _unitOfWork.AssessmentResultRepository.AddAsync(res, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetResultById), new { id = res.AssessmentResultId }, MapResultToResponse(res));
    }

    [HttpPut("assessment-results/{id:int}")]
    public async Task<ActionResult> UpdateResult(int id, [FromBody] UpdateAssessmentResultRequest request, CancellationToken cancellationToken)
    {
        if (id != request.AssessmentResultId) return BadRequest("ID không khớp.");

        var existing = await _unitOfWork.AssessmentResultRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy kết quả đánh giá với ID {id}.");

        existing.AssessmentComponentId = request.AssessmentComponentId;
        existing.LearnerId = request.LearnerId;
        existing.ETRRecordId = request.ETRRecordId;
        existing.Score = request.Score;
        existing.InstructorComment = request.InstructorComment;
        existing.RecordedBy = request.RecordedBy;
        existing.IsPublished = request.IsPublished;
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.AssessmentResultRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("assessment-results/{id:int}")]
    public async Task<ActionResult> DeleteResult(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.AssessmentResultRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy kết quả đánh giá với ID {id}.");

        _unitOfWork.AssessmentResultRepository.Delete(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("assessment-results/{id:int}/publish")]
    public async Task<ActionResult<AssessmentResultResponse>> Publish(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.AssessmentResultRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy kết quả đánh giá với ID {id}.");

        existing.IsPublished = true;
        existing.PublishedAt = DateTime.UtcNow;
        existing.ResultStatus = "Published";
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.AssessmentResultRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Ok(MapResultToResponse(existing));
    }

    private static AssessmentResultResponse MapResultToResponse(AssessmentResult r)
    {
        return new AssessmentResultResponse(
            r.AssessmentResultId,
            r.AssessmentComponentId,
            r.LearnerId,
            r.ETRRecordId,
            r.Score,
            r.ResultStatus,
            r.InstructorComment,
            r.RecordedBy,
            r.RecordedAt,
            r.PublishedAt,
            r.IsPublished);
    }
}
