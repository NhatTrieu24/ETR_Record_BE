namespace ETR.Domain.Entities;

public class ETRCourseRecord : BaseEntity
{
    public int ETRCourseRecordId { get; set; }
    public int EnrollmentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsLocked { get; set; }
    public bool CreatedBySystem { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? PreviousRecordId { get; set; }

    /// <summary>Snapshot of Course.VersionNo at Enroll time. Checklist Validation
    /// (EtrService.SubmitEtrAsync/GetCompletionProgressAsync) filters CompletionRequirement rows by
    /// this value instead of "whatever CompletionRequirement rows exist right now" — so a rule
    /// change made mid-course never retroactively fails/passes a learner who enrolled under the
    /// old rule.</summary>
    public int CourseVersionNo { get; set; } = 1;

    public ICollection<SubjectResult> SubjectResults { get; set; } = new List<SubjectResult>();
}
