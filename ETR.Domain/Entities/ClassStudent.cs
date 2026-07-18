namespace ETR.Domain.Entities;

public class ClassStudent : BaseEntity
{
    public int ClassStudentId { get; set; }
    public int CourseEnrollmentId { get; set; }
    public int ClassId { get; set; }
    public int AccountId { get; set; }
    public string Status { get; set; } = string.Empty;
}
