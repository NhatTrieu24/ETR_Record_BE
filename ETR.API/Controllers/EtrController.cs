using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api")]
public class EtrController : ControllerBase
{
    private readonly IEtrService _etrService;
    private readonly IUnitOfWork _unitOfWork;

    public EtrController(IEtrService etrService, IUnitOfWork unitOfWork)
    {
        _etrService = etrService;
        _unitOfWork = unitOfWork;
    }

    #region ETR Records

    [HttpGet("etr")]
    public async Task<ActionResult<IEnumerable<EtrRecordResponse>>> GetAllEtr(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.ETRRecordRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapEtrToResponse));
    }

    [HttpGet("etr/{id:int}")]
    public async Task<ActionResult<EtrRecordResponse>> GetEtrById(int id, CancellationToken cancellationToken)
    {
        var etr = await _unitOfWork.ETRRecordRepository.GetByIdAsync(id, cancellationToken);
        if (etr == null) return NotFound($"Không tìm thấy hồ sơ ETR với ID {id}.");
        return Ok(MapEtrToResponse(etr));
    }

    [HttpPost("etr/{id:int}/submit")]
    public async Task<ActionResult<EtrRecordResponse>> SubmitEtr(
        int id,
        [FromBody] EtrActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _etrService.SubmitEtrAsync(id, request.UserId, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("etr/{id:int}/verify")]
    public async Task<ActionResult<EtrRecordResponse>> VerifyEtr(
        int id,
        [FromBody] EtrActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _etrService.VerifyEtrAsync(id, request.UserId, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("etr/{id:int}/complete")]
    public async Task<ActionResult<EtrRecordResponse>> CompleteEtr(
        int id,
        [FromBody] EtrActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _etrService.CompleteEtrAsync(id, request.UserId, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("etr/{id:int}/lock")]
    public async Task<ActionResult<EtrRecordResponse>> LockEtr(
        int id,
        [FromBody] EtrActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _etrService.LockEtrAsync(id, request.UserId, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("etr/{id:int}/unlock")]
    public async Task<ActionResult<EtrRecordResponse>> UnlockEtr(
        int id,
        [FromBody] EtrActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _etrService.UnlockEtrAsync(id, request.UserId, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("etr/{id:int}/summary")]
    public async Task<ActionResult<EtrSummaryResponse>> GetEtrSummary(int id, CancellationToken cancellationToken)
    {
        var etr = await _unitOfWork.ETRRecordRepository.GetByIdAsync(id, cancellationToken);
        if (etr == null) return NotFound($"Không tìm thấy hồ sơ ETR với ID {id}.");

        var progressItems = (await _unitOfWork.ETRChecklistProgressRepository.GetAllAsync(cancellationToken))
            .Where(p => p.ETRRecordId == id)
            .ToList();

        int total = progressItems.Count;
        int completed = progressItems.Count(p => p.IsCompleted);
        double percentage = total > 0 ? (double)completed / total * 100 : 0;

        return Ok(new EtrSummaryResponse(etr.ETRRecordId, etr.Status, etr.IsLocked, total, completed, percentage));
    }

    [HttpGet("etr/{id:int}/progress")]
    public async Task<ActionResult<IEnumerable<ChecklistProgressResponse>>> GetEtrProgress(int id, CancellationToken cancellationToken)
    {
        var progressItems = (await _unitOfWork.ETRChecklistProgressRepository.GetAllAsync(cancellationToken))
            .Where(p => p.ETRRecordId == id)
            .ToList();

        return Ok(progressItems.Select(MapProgressToResponse));
    }

    #endregion

    #region Checklist Progress

    [HttpGet("checklist-progress")]
    public async Task<ActionResult<IEnumerable<ChecklistProgressResponse>>> GetChecklistProgress(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.ETRChecklistProgressRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapProgressToResponse));
    }

    [HttpPut("checklist-progress/{id:int}")]
    public async Task<ActionResult<ChecklistProgressResponse>> UpdateChecklistProgress(
        int id,
        [FromBody] UpdateChecklistProgressRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _etrService.UpdateChecklistProgressAsync(
                id,
                request.IsCompleted,
                request.VerifiedByUserId,
                request.Comment,
                cancellationToken);

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("checklist-progress")]
    public async Task<ActionResult<ChecklistProgressResponse>> UpdateChecklistProgressQuery(
        [FromQuery] int progressId,
        [FromBody] UpdateChecklistProgressRequest request,
        CancellationToken cancellationToken)
    {
        return await UpdateChecklistProgress(progressId, request, cancellationToken);
    }

    #endregion

    private static EtrRecordResponse MapEtrToResponse(ETRRecord e)
    {
        return new EtrRecordResponse(
            e.ETRRecordId,
            e.EnrollmentId,
            e.Status,
            e.IsLocked,
            e.SubmittedAt,
            e.VerifiedAt,
            e.CompletedAt);
    }

    private static ChecklistProgressResponse MapProgressToResponse(ETRChecklistProgress p)
    {
        return new ChecklistProgressResponse(
            p.ProgressId,
            p.ETRRecordId,
            p.ChecklistItemId,
            p.IsCompleted,
            p.VerifiedBy,
            p.CompletedAt,
            p.VerificationComment);
    }
}