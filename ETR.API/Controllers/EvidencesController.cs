using ETR.Application.DTOs.Evidence.Requests;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Document Management
/// [Core Responsibility]: Manages evidence file references (Cloudinary URLs) for practical checklists and assessments.
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
    [Authorize(Roles = "Instructor,QA,Admin,Academic,Audit")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var files = await _evidenceService.GetAllEvidencesAsync(cancellationToken);
        return Ok(files);
    }

    /// <summary>
    /// Lấy thông tin một tệp bằng chứng cụ thể theo ID.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Instructor,QA,Admin,Academic,Audit")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var file = await _evidenceService.GetEvidenceByIdAsync(id, cancellationToken);
        return Ok(file);
    }

    /// <summary>
    /// Chuyển hướng (302) đến URL Cloudinary của tệp bằng chứng — file được lưu trữ ngoài server,
    /// backend chỉ giữ URL tham chiếu (xem EvidenceService/Attachment).
    /// </summary>
    [HttpGet("{id}/download")]
    [Authorize(Roles = "Instructor,QA,Admin,Academic,Audit")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var file = await _evidenceService.GetEvidenceByIdAsync(id, cancellationToken);

        if (string.IsNullOrEmpty(file.FileUrl))
            return NotFound("Evidence file has no URL on record.");

        return Redirect(file.FileUrl);
    }

    /// <summary>
    /// Đăng ký một tệp bằng chứng đã được FE upload thẳng lên Cloudinary — request chỉ chứa URL +
    /// metadata, không có file byte nào được gửi lên backend.
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Roles = "Instructor,Admin,Academic")]
    public async Task<IActionResult> UploadEvidence([FromBody] UploadEvidenceRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var response = await _evidenceService.UploadEvidenceAsync(request, accountId, _currentUserService.RoleName, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.EvidenceFileId }, response);
    }

    /// <summary>
    /// Phê duyệt hoặc từ chối một tệp bằng chứng (Dành cho QA/Admin — Instructor không được tự
    /// verify minh chứng do chính mình tải lên, xem EvidenceService.VerifyEvidenceAsync).
    /// </summary>
    [HttpPut("{id}/verify")]
    [Authorize(Roles = "QA,Admin")]
    public async Task<IActionResult> VerifyEvidence(int id, [FromBody] VerifyEvidenceRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var response = await _evidenceService.VerifyEvidenceAsync(id, request, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Phê duyệt hoặc từ chối NHIỀU tệp bằng chứng cùng lúc với 1 trạng thái/lý do chung — dành
    /// cho QA xử lý hàng loạt (VD: 1 lớp 30 học viên × 3 file = 90 minh chứng) thay vì phải gọi
    /// PUT /{id}/verify lặp lại từng file. Item nào lỗi (không tồn tại, tự verify minh chứng của
    /// chính mình...) sẽ nằm trong "Failed" của response, KHÔNG làm rollback các item còn lại.
    /// </summary>
    [HttpPut("bulk-verify")]
    [Authorize(Roles = "QA,Admin")]
    public async Task<IActionResult> BulkVerifyEvidence([FromBody] BulkVerifyEvidenceRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var response = await _evidenceService.BulkVerifyEvidencesAsync(request, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Xóa mềm (soft delete) một tệp bằng chứng.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Instructor,Admin,Academic")]
    public async Task<IActionResult> DeleteEvidence(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        await _evidenceService.DeleteEvidenceAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}
