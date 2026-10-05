namespace ETR.Application.DTOs.Evidence;

public class EvidenceResponse
{
    public int EvidenceFileId { get; set; }
    public int EvidenceTypeId { get; set; }
    public int UploadedByAccountId { get; set; }
    public int AccountId { get; set; }
    public int SubjectResultId { get; set; }
    public int? AttendanceRecordId { get; set; }
    public int? AssessmentResultId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long? FileSize { get; set; }
    public string? FileHash { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public int? VerifiedByAccountId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationComment { get; set; }
    public DateTime UploadedAt { get; set; }

    // Enriched metadata fields for UI display (QA verification, Instructor view, Auditor)
    public string? EvidenceTypeName { get; set; }
    public string? UploadedByName { get; set; }
    public string? LearnerName { get; set; }
    public string? LearnerCode { get; set; }
    public string? SubjectName { get; set; }
    public string? SubjectCode { get; set; }
    public string? CourseName { get; set; }
    public string? ClassName { get; set; }
}
