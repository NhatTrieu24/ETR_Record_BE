namespace ETR.Domain.Enums;

/// <summary>Every value ever written to SubjectResult.Status. Column stays nvarchar in the DB
/// (HasConversion&lt;string&gt; in AppDbContext).</summary>
public enum SubjectResultStatus
{
    Pending,
    Passed,
    Failed,
    Exempted
}
