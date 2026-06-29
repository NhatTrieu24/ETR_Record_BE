namespace ETR.Domain.Entities;

public class DashboardSnapshot : BaseEntity
{
    public int SnapshotId { get; set; }
    public int? CourseId { get; set; }
    public DateTime SnapshotDate { get; set; }
    public int TotalLearners { get; set; }
    public int TotalClasses { get; set; }
    public int TotalETRs { get; set; }
    public int CompletedETRs { get; set; }
    public int PendingETRs { get; set; }
    public int RejectedETRs { get; set; }
    public int MissingEvidenceETRs { get; set; }
    public decimal AverageAttendanceRate { get; set; }
    public decimal AverageAssessmentScore { get; set; }
    public DateTime GeneratedAt { get; set; }
}
