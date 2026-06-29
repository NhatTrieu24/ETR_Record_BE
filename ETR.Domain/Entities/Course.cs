namespace ETR.Domain.Entities;

public class Course : BaseEntity
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationHours { get; set; }
    public string Status { get; set; } = string.Empty;
}
