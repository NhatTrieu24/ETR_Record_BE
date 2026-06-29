namespace ETR.Domain.Entities;

public class CompletionRequirement : BaseEntity
{
    public int RequirementId { get; set; }
    public int CourseId { get; set; }
    public string RequirementName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsMandatory { get; set; }
    public int DisplayOrder { get; set; }
}
