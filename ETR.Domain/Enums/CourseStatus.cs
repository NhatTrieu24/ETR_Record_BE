namespace ETR.Domain.Enums;

/// <summary>Every value ever written to Course.Status. Column stays nvarchar in the DB (HasConversion&lt;string&gt;
/// in AppDbContext) — this enum only replaces raw string literals at call sites so a typo can't silently
/// create a new, uncomparable value.</summary>
public enum CourseStatus
{
    Active,
    Inactive
}
