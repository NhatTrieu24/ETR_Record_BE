namespace ETR.Application.DTOs;

public record EvidenceFileResponse(
    int EvidenceFileId,
    int EvidenceTypeId,
    string FileName,
    string FilePath,
    string? FileExtension,
    string? MimeType,
    long FileSize,
    string VerificationStatus,
    string? QAComment,
    int? VerifiedBy,
    DateTime? VerifiedAt,
    int UploadedBy,
    DateTime UploadedAt);
