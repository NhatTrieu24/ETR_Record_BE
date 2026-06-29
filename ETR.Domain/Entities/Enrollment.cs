namespace ETR.Domain.Entities;

public class Enrollment : BaseEntity
{
    public int EnrollmentId { get; set; }
    public int LearnerId { get; set; }
    public int ClassId { get; set; }
    public DateTime EnrolledAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
