namespace ETR.Domain.Entities;

public class SubjectResult : BaseEntity
{
    public int SubjectResultId { get; set; }
    public int EtrId { get; set; }
    public int CourseId { get; set; }
    public int SubjectId { get; set; }
    public decimal? AttendanceRate { get; set; }
    public decimal? Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? EvaluatedByAccountId { get; set; }
    public DateTime? EvaluatedAt { get; set; }
}
