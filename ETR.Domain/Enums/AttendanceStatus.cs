namespace ETR.Domain.Enums;

/// <summary>Every value ever written to AttendanceRecord.Status. Column stays nvarchar in the DB
/// (HasConversion&lt;string&gt; in AppDbContext).</summary>
public enum AttendanceStatus
{
    Present,
    Absent
}
