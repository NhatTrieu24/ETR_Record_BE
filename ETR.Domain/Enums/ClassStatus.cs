namespace ETR.Domain.Enums;

/// <summary>Every value ever written to Class.Status. Column stays nvarchar in the DB
/// (HasConversion&lt;string&gt; in AppDbContext).</summary>
public enum ClassStatus
{
    Planned,
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}
