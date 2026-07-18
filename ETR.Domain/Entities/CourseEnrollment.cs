namespace ETR.Domain.Entities;

public class CourseEnrollment : BaseEntity
{
    public int EnrollmentId { get; set; }
    public int AccountId { get; set; }
    public int ClassId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpectedCompletionDate { get; set; }
    public DateTime? ActualCompletionDate { get; set; }
}
