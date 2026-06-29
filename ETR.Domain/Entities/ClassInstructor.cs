namespace ETR.Domain.Entities;

public class ClassInstructor : BaseEntity
{
    public int ClassInstructorId { get; set; }
    public int ClassId { get; set; }
    public int UserId { get; set; }
    public DateTime AssignedAt { get; set; }
    public bool IsPrimaryInstructor { get; set; }
}
