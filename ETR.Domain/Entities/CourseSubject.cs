namespace ETR.Domain.Entities;

public class CourseSubject : BaseEntity
{
    public int CourseId { get; set; }
    public int SubjectId { get; set; }
    public int SequenceNo { get; set; }
    public int RequiredHours { get; set; }
    public decimal PassingScore { get; set; }
    public bool IsMandatory { get; set; }
    public string? SubjectVersion { get; set; }
}
