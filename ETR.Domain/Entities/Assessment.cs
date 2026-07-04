namespace ETR.Domain.Entities;

public class Assessment : BaseEntity
{
    public int AssessmentId { get; set; }
    public int CourseId { get; set; }
    public int SubjectId { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal PassingScore { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}
