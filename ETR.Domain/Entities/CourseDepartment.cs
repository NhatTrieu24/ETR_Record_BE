namespace ETR.Domain.Entities;

/// <summary>
/// Defines which training audience departments are eligible to enroll in a Course.
/// If a Course has no CourseDepartment entries, it is open to learners from all departments
/// (backward-compatible / unconstrained).
/// </summary>
public class CourseDepartment : BaseEntity
{
    public int CourseId { get; set; }
    public int DepartmentId { get; set; }

    public Course Course { get; set; } = null!;
    public Department Department { get; set; } = null!;
}
