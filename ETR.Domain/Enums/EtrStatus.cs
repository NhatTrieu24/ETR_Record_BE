namespace ETR.Domain.Enums;

/// <summary>Every value ever written to ETRCourseRecord.Status. Column stays nvarchar in the DB
/// (HasConversion&lt;string&gt; in AppDbContext).</summary>
public enum EtrStatus
{
    Draft,
    InProgress,
    Submitted,
    Verified,
    Completed,
    ReturnedForCorrection,
    Cancelled
}
