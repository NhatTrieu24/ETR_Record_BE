namespace ETR.Domain.Enums;

/// <summary>Every value ever written to CourseEnrollment.Status. Column stays nvarchar in the DB
/// (HasConversion&lt;string&gt; in AppDbContext).</summary>
public enum EnrollmentStatus
{
    Active,
    Enrolled,
    Withdrawn,
    Completed,
    Deleted
}
