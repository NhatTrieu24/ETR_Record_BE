using ETR.Application.DTOs.Evidence;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Document Management
/// [Core Responsibility]: Manages uploaded evidence files for practical checklists and assessments.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EvidencesController : ControllerBase
{
    private readonly IEvidenceService _evidenceService;
    private readonly ICurrentUserService _currentUserService;

    public EvidencesController(IEvidenceService evidenceService, ICurrentUserService currentUserService)
    {
        _evidenceService = evidenceService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Lấy danh sách tất cả các tệp bằng chứng (evidence) đã tải lên.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var files = await _evidenceService.GetAllEvidencesAsync(cancellationToken);
        return Ok(files);
    }

    /// <summary>
    /// Lấy thông tin một tệp bằng chứng cụ thể theo ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var file = await _evidenceService.GetEvidenceByIdAsync(id, cancellationToken);
        return Ok(file);
    }

    /// <summary>
    /// Tải lên một bản ghi tệp bằng chứng mới.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UploadEvidence([FromBody] CreateEvidenceRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var response = await _evidenceService.UploadEvidenceAsync(request, accountId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.EvidenceFileId }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvidence(int id, [FromBody] ETR.Application.DTOs.UpdateEvidenceRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var response = await _evidenceService.UpdateEvidenceAsync(id, request, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Xóa mềm (soft delete) một tệp bằng chứng.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvidence(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        await _evidenceService.DeleteEvidenceAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}
