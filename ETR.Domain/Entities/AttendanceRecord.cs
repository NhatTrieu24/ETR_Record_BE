namespace ETR.Domain.Entities;

public class AttendanceRecord : BaseEntity
{
    public int AttendanceRecordId { get; set; }
    public int SessionId { get; set; }

    /// <summary>FK to CourseEnrollment.EnrollmentId. Used to point directly at the Class-Enrollment
    /// relationship instead of through the (now removed) ClassStudent indirection table — see
    /// mục #10, docs/todo/9.todo_to_complete_system.md.</summary>
    public int EnrollmentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public int RecordedByAccountId { get; set; }
    public DateTime RecordedAt { get; set; }
}
