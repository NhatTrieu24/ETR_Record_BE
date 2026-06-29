using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/etr")]
public class EtrController : ControllerBase
{
    private readonly IEtrService _etrService;

    public EtrController(IEtrService etrService)
    {
        _etrService = etrService;
    }

    // 1. Cập nhật tiến độ của một đầu mục Checklist
    [HttpPut("checklist/{progressId:int}")]
    public async Task<ActionResult<ETRChecklistProgress>> UpdateChecklistProgress(
        int progressId,
        [FromBody] UpdateChecklistProgressRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var progress = await _etrService.UpdateChecklistProgressAsync(
                progressId,
                request.IsCompleted,
                request.VerifiedByUserId,
                request.Comment,
                cancellationToken);

            return Ok(progress);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex) // Chặn khi ETR đã bị khóa/hoàn thành ở Chặng 3
        {
            return BadRequest(ex.Message);
        }
    }

    // 2. Endpoint Nộp hồ sơ ETR (Submit)
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<ETRRecord>> SubmitEtr(
        int id,
        [FromBody] EtrActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var etr = await _etrService.SubmitEtrAsync(id, request.UserId, cancellationToken);
            return Ok(etr);
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

    // 3. Endpoint Xác minh hồ sơ ETR (QA Verify)
    [HttpPost("{id:int}/verify")]
    public async Task<ActionResult<ETRRecord>> VerifyEtr(
        int id,
        [FromBody] EtrActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var etr = await _etrService.VerifyEtrAsync(id, request.UserId, cancellationToken);
            return Ok(etr);
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

    // 4. Endpoint Hoàn thành & Khóa cứng hồ sơ (Complete & Lock)
    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<ETRRecord>> CompleteEtr(
        int id,
        [FromBody] EtrActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var etr = await _etrService.CompleteEtrAsync(id, request.UserId, cancellationToken);
            return Ok(etr);
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
}

// --- ĐỊNH NGHĨA CÁC REQUEST DTOs ---

public record UpdateChecklistProgressRequest(
    bool IsCompleted,
    int? VerifiedByUserId,
    string? Comment);

public record EtrActionRequest(int UserId);