namespace ETR.Domain.Entities;

public class AttendanceSession : BaseEntity
{
    public int AttendanceSessionId { get; set; }
    public int ClassId { get; set; }
    public string SessionTitle { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public string? Location { get; set; }
    public bool IsConfirmed { get; set; }
    public int? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}
