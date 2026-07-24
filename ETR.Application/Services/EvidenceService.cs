using ETR.Application.Compliance;
using ETR.Application.DTOs.Evidence;
using ETR.Application.DTOs.Evidence.Requests;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace ETR.Application.Services;

public class EvidenceService : IEvidenceService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "application/pdf"
    };

    private static readonly HashSet<string> AllowedVerificationStatuses = new(StringComparer.Ordinal)
    {
        "Verified", "Rejected"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly IUnitOfWork _unitOfWork;

    public EvidenceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<EvidenceResponse>> GetAllEvidencesAsync(CancellationToken cancellationToken = default)
    {
        var evidences = await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken);
        return evidences.Select(MapToResponse).ToList();
    }

    public async Task<EvidenceResponse> GetEvidenceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var evidence = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (evidence == null)
            throw new KeyNotFoundException($"Evidence with ID {id} not found.");

        return MapToResponse(evidence);
    }

    public async Task<EvidenceResponse> UploadEvidenceAsync(UploadEvidenceRequest request, int uploadedByAccountId, string webRootPath, CancellationToken cancellationToken = default)
    {
        if (request.File == null || request.File.Length == 0)
            throw new ValidationException("File is empty or not provided.");

        if (request.File.Length > MaxFileSizeBytes)
            throw new ValidationException($"File exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.");

        var fileExtension = Path.GetExtension(request.File.FileName);
        if (string.IsNullOrEmpty(fileExtension) || !AllowedExtensions.Contains(fileExtension))
            throw new ValidationException($"File extension '{fileExtension}' is not allowed. Allowed extensions: {string.Join(", ", AllowedExtensions)}.");

        if (string.IsNullOrEmpty(request.File.ContentType) || !AllowedMimeTypes.Contains(request.File.ContentType))
            throw new ValidationException($"File content type '{request.File.ContentType}' is not allowed.");

        var uploadDir = Path.Combine(webRootPath, "uploads", "evidences");
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(uploadDir, uniqueFileName);

        try
        {
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // System.IO throws the same UnauthorizedAccessException type as auth failures on OS-level
            // permission denial — translate both IO failure modes to a domain error so the client sees
            // a storage-specific message instead of a misleading 401 "Not authenticated".
            throw new BusinessRuleViolationException("Could not save the uploaded file. Please retry or contact an administrator.");
        }

        var relativePath = Path.Combine("uploads", "evidences", uniqueFileName).Replace("\\", "/");

        // Treat 0 or negative as null for nullable FK fields to avoid FK violations
        var attendanceRecordId = request.AttendanceRecordId.HasValue && request.AttendanceRecordId.Value > 0
            ? request.AttendanceRecordId
            : null;
        var assessmentResultId = request.AssessmentResultId.HasValue && request.AssessmentResultId.Value > 0
            ? request.AssessmentResultId
            : null;

        var evidence = new EvidenceFile
        {
            EvidenceTypeId = request.EvidenceTypeId,
            AccountId = request.AccountId,
            SubjectResultId = request.SubjectResultId,
            AttendanceRecordId = attendanceRecordId,
            AssessmentResultId = assessmentResultId,
            FileName = request.File.FileName,
            FilePath = relativePath,
            FileExtension = fileExtension,
            MimeType = request.File.ContentType,
            FileSize = request.File.Length,
            VerificationStatus = "Pending", // Default value
            UploadedByAccountId = uploadedByAccountId,
            UploadedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = uploadedByAccountId
        };

        await _unitOfWork.EvidenceFileRepository.AddAsync(evidence, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(evidence);
    }

    public async Task<EvidenceResponse> VerifyEvidenceAsync(int id, VerifyEvidenceRequest request, int verifiedByAccountId, CancellationToken cancellationToken = default)
    {
        if (!AllowedVerificationStatuses.Contains(request.VerificationStatus))
            throw new ValidationException($"VerificationStatus must be one of: {string.Join(", ", AllowedVerificationStatuses)}.");

        if (request.VerificationStatus == "Rejected" && string.IsNullOrWhiteSpace(request.VerificationComment))
            throw new ValidationException("A comment is required when rejecting evidence.");

        var evidence = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (evidence == null)
            throw new KeyNotFoundException($"Evidence with ID {id} not found.");

        evidence.VerificationStatus = request.VerificationStatus;
        evidence.VerificationComment = request.VerificationComment;
        evidence.VerifiedByAccountId = verifiedByAccountId;
        evidence.VerifiedAt = DateTime.UtcNow;

        evidence.UpdatedAt = DateTime.UtcNow;
        evidence.UpdatedByAccountId = verifiedByAccountId;

        _unitOfWork.EvidenceFileRepository.Update(evidence);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(evidence);
    }

    public async Task DeleteEvidenceAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var evidence = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (evidence == null)
            throw new KeyNotFoundException($"Evidence with ID {id} not found.");

        // Soft delete based on BaseEntity pattern
        evidence.IsDeleted = true;
        evidence.DeletedAt = DateTime.UtcNow;
        evidence.UpdatedAt = DateTime.UtcNow;
        evidence.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.EvidenceFileRepository.Update(evidence);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private EvidenceResponse MapToResponse(EvidenceFile file)
    {
        return new EvidenceResponse
        {
            EvidenceFileId = file.EvidenceFileId,
            EvidenceTypeId = file.EvidenceTypeId,
            UploadedByAccountId = file.UploadedByAccountId,
            AccountId = file.AccountId,
            SubjectResultId = file.SubjectResultId,
            AttendanceRecordId = file.AttendanceRecordId,
            AssessmentResultId = file.AssessmentResultId,
            FileName = file.FileName,
            FilePath = file.FilePath,
            FileExtension = file.FileExtension,
            MimeType = file.MimeType,
            FileSize = file.FileSize,
            VerificationStatus = file.VerificationStatus,
            VerifiedByAccountId = file.VerifiedByAccountId,
            VerifiedAt = file.VerifiedAt,
            VerificationComment = file.VerificationComment,
            UploadedAt = file.UploadedAt
        };
    }
}
