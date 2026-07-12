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
}
