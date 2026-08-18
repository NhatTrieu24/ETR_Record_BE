namespace ETR.Domain.Enums;

/// <summary>Every value ever written to AmendmentRequest.Status. Column stays nvarchar in the DB
/// (HasConversion&lt;string&gt; in AppDbContext).</summary>
public enum AmendmentStatus
{
    Pending,
    Approved,
    Rejected
}
