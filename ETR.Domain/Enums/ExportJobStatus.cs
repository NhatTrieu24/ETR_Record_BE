namespace ETR.Domain.Enums;

/// <summary>Every value ever written to ExportJob.Status. Column stays nvarchar in the DB
/// (HasConversion&lt;string&gt; in AppDbContext).</summary>
public enum ExportJobStatus
{
    InProgress,
    Completed,
    Failed
}
