namespace ETR.Domain.Entities;

public class AttendanceRecord : BaseEntity
{
    public int AttendanceRecordId { get; set; }
    public int SessionId { get; set; }
    public int LearnerId { get; set; }
    public int EnrollmentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public int RecordedBy { get; set; }
    public DateTime RecordedAt { get; set; }
}
