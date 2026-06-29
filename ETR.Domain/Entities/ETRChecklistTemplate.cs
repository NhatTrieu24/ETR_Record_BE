namespace ETR.Domain.Entities;

public class ETRChecklistTemplate : BaseEntity
{
    public int TemplateId { get; set; }
    public int CourseId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int VersionNo { get; set; }
    public bool IsActive { get; set; }
}
