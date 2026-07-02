namespace ETR.Application.DTOs;

public record CreateEvidenceRequest(
    int EvidenceTypeId,
    string FileName,
    string FilePath,
    string? FileExtension,
    string? MimeType,
    long FileSize,
    int UploadedBy,
    int LearnerId,
    int ETRRecordId,
    int? AttendanceRecordId,
    int? AssessmentResultId);
