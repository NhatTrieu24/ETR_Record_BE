namespace ETR.Domain.Entities;

public class EvidenceFile : BaseEntity
{
    public int EvidenceFileId { get; set; }
    public int EvidenceTypeId { get; set; }
    public int UploadedByAccountId { get; set; }
    public int AccountId { get; set; }
    public int SubjectResultId { get; set; }
    public int? AttendanceRecordId { get; set; }
    public int? AssessmentResultId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? FileExtension { get; set; }
    public string? MimeType { get; set; }
    public long FileSize { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public int? VerifiedByAccountId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationComment { get; set; }
    public DateTime UploadedAt { get; set; }
}
