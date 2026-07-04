namespace ETR.Domain.Entities;

public class EvidenceFile : BaseEntity
{
    public int EvidenceFileId { get; set; }
    public int EvidenceTypeId { get; set; }
    public int UploadedBy { get; set; }
    public int LearnerId { get; set; }
    public int SubjectResultId { get; set; }
    public int? AttendanceRecordId { get; set; }
    public int? AssessmentResultId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? FileExtension { get; set; }
    public string? MimeType { get; set; }
    public long FileSize { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public int? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationComment { get; set; }
    public DateTime UploadedAt { get; set; }
}
