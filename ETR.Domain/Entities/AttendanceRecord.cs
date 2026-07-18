namespace ETR.Domain.Entities;

public class AttendanceRecord : BaseEntity
{
    public int AttendanceRecordId { get; set; }
    public int SessionId { get; set; }
    public int ClassStudentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public int RecordedByAccountId { get; set; }
    public DateTime RecordedAt { get; set; }
}
