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

    // File metadata (Url/FileName/MimeType/FileSize) lives on the polymorphic Attachment table now
    // (OwnerType = nameof(EvidenceFile), OwnerId = EvidenceFileId) — see Attachment.cs.
    public string VerificationStatus { get; set; } = string.Empty;
    public int? VerifiedByAccountId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationComment { get; set; }
    public DateTime UploadedAt { get; set; }
}
