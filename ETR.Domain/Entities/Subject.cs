namespace ETR.Domain.Entities;

public class Subject : BaseEntity
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public int DefaultHours { get; set; }
    public string? AssessmentMethod { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
}
