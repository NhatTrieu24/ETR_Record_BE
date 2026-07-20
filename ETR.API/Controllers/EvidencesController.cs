using ETR.Application.DTOs.Evidence.Requests;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;

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
    private readonly IWebHostEnvironment _env;

    public EvidencesController(IEvidenceService evidenceService, ICurrentUserService currentUserService, IWebHostEnvironment env)
    {
        _evidenceService = evidenceService;
        _currentUserService = currentUserService;
        _env = env;
    }

    /// <summary>
    /// Lấy danh sách tất cả các tệp bằng chứng (evidence) đã tải lên.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Instructor,QA,Admin")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var files = await _evidenceService.GetAllEvidencesAsync(cancellationToken);
        return Ok(files);
    }

    /// <summary>
    /// Lấy thông tin một tệp bằng chứng cụ thể theo ID.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Instructor,QA,Admin")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var file = await _evidenceService.GetEvidenceByIdAsync(id, cancellationToken);
        return Ok(file);
    }

    /// <summary>
    /// Tải xuống tệp bằng chứng vật lý theo ID.
    /// </summary>
    [HttpGet("{id}/download")]
    [Authorize(Roles = "Instructor,QA,Admin")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var file = await _evidenceService.GetEvidenceByIdAsync(id, cancellationToken);
        
        if (string.IsNullOrEmpty(file.FilePath))
            return NotFound("File path is empty.");

        var physicalPath = Path.Combine(_env.WebRootPath, file.FilePath);
        
        if (!System.IO.File.Exists(physicalPath))
            return NotFound("Physical file not found on disk.");

        var mimeType = file.MimeType ?? "application/octet-stream";
        var fileName = file.FileName ?? Path.GetFileName(physicalPath);

        return PhysicalFile(physicalPath, mimeType, fileName);
    }

    /// <summary>
    /// Tải lên một bản ghi tệp bằng chứng mới.
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> UploadEvidence([FromForm] UploadEvidenceRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var webRootPath = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRootPath))
        {
            webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var response = await _evidenceService.UploadEvidenceAsync(request, accountId, webRootPath, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.EvidenceFileId }, response);
    }

    /// <summary>
    /// Phê duyệt hoặc từ chối một tệp bằng chứng (Dành cho Instructor/Admin).
    /// </summary>
    [HttpPut("{id}/verify")]
    [Authorize(Roles = "Instructor,QA,Admin")]
    public async Task<IActionResult> VerifyEvidence(int id, [FromBody] VerifyEvidenceRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var response = await _evidenceService.VerifyEvidenceAsync(id, request, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Xóa mềm (soft delete) một tệp bằng chứng.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> DeleteEvidence(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        await _evidenceService.DeleteEvidenceAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}
