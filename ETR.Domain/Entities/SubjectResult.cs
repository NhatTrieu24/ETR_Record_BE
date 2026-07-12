namespace ETR.Domain.Entities;

public class SubjectResult : BaseEntity
{
    public int SubjectResultId { get; set; }
    public int EnrollmentId { get; set; }
    public int CourseId { get; set; }
    public int SubjectId { get; set; }
    public decimal? AttendanceRate { get; set; }
    public decimal? Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? EvaluatedBy { get; set; }
    public DateTime? EvaluatedAt { get; set; }
}
