using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api")]
public class EvidencesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public EvidencesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    #region Evidence Type Management

    [HttpGet("evidence-types")]
    public async Task<ActionResult<IEnumerable<EvidenceTypeResponse>>> GetEvidenceTypes(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.EvidenceTypeRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapTypeToResponse));
    }

    [HttpGet("evidence-types/{id:int}")]
    public async Task<ActionResult<EvidenceTypeResponse>> GetEvidenceTypeById(int id, CancellationToken cancellationToken)
    {
        var type = await _unitOfWork.EvidenceTypeRepository.GetByIdAsync(id, cancellationToken);
        if (type == null) return NotFound($"Không tìm thấy loại minh chứng với ID {id}.");
        return Ok(MapTypeToResponse(type));
    }

    [HttpPost("evidence-types")]
    public async Task<ActionResult<EvidenceTypeResponse>> CreateEvidenceType([FromBody] CreateEvidenceTypeRequest request, CancellationToken cancellationToken)
    {
        var type = new EvidenceType
        {
            TypeName = request.TypeName,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.EvidenceTypeRepository.AddAsync(type, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetEvidenceTypeById), new { id = type.EvidenceTypeId }, MapTypeToResponse(type));
    }

    [HttpPut("evidence-types/{id:int}")]
    public async Task<ActionResult> UpdateEvidenceType(int id, [FromBody] UpdateEvidenceTypeRequest request, CancellationToken cancellationToken)
    {
        if (id != request.EvidenceTypeId) return BadRequest("ID không khớp.");

        var existing = await _unitOfWork.EvidenceTypeRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy loại minh chứng với ID {id}.");

        existing.TypeName = request.TypeName;
        existing.Description = request.Description;
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EvidenceTypeRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("evidence-types/{id:int}")]
    public async Task<ActionResult> DeleteEvidenceType(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.EvidenceTypeRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy loại minh chứng với ID {id}.");

        _unitOfWork.EvidenceTypeRepository.Delete(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    #endregion

    #region Evidence File (Evidences)

    [HttpGet("evidences")]
    public async Task<ActionResult<IEnumerable<EvidenceFileResponse>>> GetEvidences(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapFileToResponse));
    }

    [HttpGet("evidences/{id:int}")]
    public async Task<ActionResult<EvidenceFileResponse>> GetEvidenceById(int id, CancellationToken cancellationToken)
    {
        var evidence = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (evidence == null) return NotFound($"Không tìm thấy minh chứng với ID {id}.");
        return Ok(MapFileToResponse(evidence));
    }

    [HttpPost("evidences")]
    public async Task<ActionResult<EvidenceFileResponse>> CreateEvidence([FromBody] CreateEvidenceRequest request, CancellationToken cancellationToken)
    {
        var evidence = new EvidenceFile
        {
            EvidenceTypeId = request.EvidenceTypeId,
            FileName = request.FileName,
            FilePath = request.FilePath,
            FileExtension = request.FileExtension,
            MimeType = request.MimeType,
            FileSize = request.FileSize,
            UploadedBy = request.UploadedBy,
            LearnerId = request.LearnerId,
            ETRRecordId = request.ETRRecordId,
            AttendanceRecordId = request.AttendanceRecordId,
            AssessmentResultId = request.AssessmentResultId,
            CreatedAt = DateTime.UtcNow,
            UploadedAt = DateTime.UtcNow,
            IsDeleted = false,
            VerificationStatus = "Pending"
        };

        await _unitOfWork.EvidenceFileRepository.AddAsync(evidence, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetEvidenceById), new { id = evidence.EvidenceFileId }, MapFileToResponse(evidence));
    }

    [HttpPut("evidences/{id:int}")]
    public async Task<ActionResult> UpdateEvidence(int id, [FromBody] UpdateEvidenceRequest request, CancellationToken cancellationToken)
    {
        if (id != request.EvidenceFileId) return BadRequest("ID không khớp.");

        var existing = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy minh chứng với ID {id}.");

        existing.EvidenceTypeId = request.EvidenceTypeId;
        existing.FileName = request.FileName;
        existing.FilePath = request.FilePath;
        existing.FileExtension = request.FileExtension;
        existing.MimeType = request.MimeType;
        existing.FileSize = request.FileSize;
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EvidenceFileRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("evidences/{id:int}")]
    public async Task<ActionResult> DeleteEvidence(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy minh chứng với ID {id}.");

        _unitOfWork.EvidenceFileRepository.Delete(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("evidences/upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<EvidenceFileResponse>> UploadEvidence(
        [FromForm] UploadEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0) return BadRequest("Tệp tải lên không hợp lệ.");

        // Simulate local file storage path
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileName = Path.GetFileName(request.File.FileName);
        var fileExtension = Path.GetExtension(request.File.FileName);
        var filePath = Path.Combine(folderPath, Guid.NewGuid().ToString() + fileExtension);

        var evidence = new EvidenceFile
        {
            EvidenceTypeId = request.EvidenceTypeId,
            UploadedBy = request.UploadedBy,
            LearnerId = request.LearnerId,
            ETRRecordId = request.ETRRecordId,
            AttendanceRecordId = request.AttendanceRecordId,
            AssessmentResultId = request.AssessmentResultId,
            FileName = fileName,
            FilePath = filePath,
            FileExtension = fileExtension,
            MimeType = request.File.ContentType,
            FileSize = request.File.Length,
            VerificationStatus = "Pending",
            UploadedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.EvidenceFileRepository.AddAsync(evidence, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetEvidenceById), new { id = evidence.EvidenceFileId }, MapFileToResponse(evidence));
    }

    [HttpPost("evidences/{id:int}/verify")]
    public async Task<ActionResult<EvidenceFileResponse>> VerifyEvidence(int id, [FromBody] EvidenceActionRequest request, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy minh chứng với ID {id}.");

        existing.VerificationStatus = "Verified";
        existing.QAComment = request.Comment;
        existing.VerifiedBy = request.VerifiedByUserId;
        existing.VerifiedAt = DateTime.UtcNow;
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EvidenceFileRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Ok(MapFileToResponse(existing));
    }

    [HttpPost("evidences/{id:int}/reject")]
    public async Task<ActionResult<EvidenceFileResponse>> RejectEvidence(int id, [FromBody] EvidenceActionRequest request, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy minh chứng với ID {id}.");

        existing.VerificationStatus = "Rejected";
        existing.QAComment = request.Comment;
        existing.VerifiedBy = request.VerifiedByUserId;
        existing.VerifiedAt = DateTime.UtcNow;
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EvidenceFileRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Ok(MapFileToResponse(existing));
    }

    [HttpGet("evidences/download/{id:int}")]
    public async Task<IActionResult> DownloadEvidence(int id, CancellationToken cancellationToken)
    {
        var evidence = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (evidence == null) return NotFound($"Không tìm thấy minh chứng với ID {id}.");

        var mockFileContent = $"Nội dung minh chứng giả lập cho ETR Record: {evidence.ETRRecordId}, Tên tệp: {evidence.FileName}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(mockFileContent);
        var stream = new System.IO.MemoryStream(bytes);

        return File(stream, evidence.MimeType ?? "application/octet-stream", evidence.FileName);
    }

    #endregion

    private static EvidenceTypeResponse MapTypeToResponse(EvidenceType t)
    {
        return new EvidenceTypeResponse(
            t.EvidenceTypeId,
            t.TypeName,
            t.Description);
    }

    private static EvidenceFileResponse MapFileToResponse(EvidenceFile f)
    {
        return new EvidenceFileResponse(
            f.EvidenceFileId,
            f.EvidenceTypeId,
            f.FileName,
            f.FilePath,
            f.FileExtension,
            f.MimeType,
            f.FileSize,
            f.VerificationStatus,
            f.QAComment,
            f.VerifiedBy,
            f.VerifiedAt,
            f.UploadedBy,
            f.UploadedAt);
    }
}
