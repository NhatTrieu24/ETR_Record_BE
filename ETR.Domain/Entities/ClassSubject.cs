namespace ETR.Domain.Entities;

public class ClassSubject : BaseEntity
{
    public int ClassSubjectId { get; set; }
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
    public int? InstructorAccountId { get; set; }
}
