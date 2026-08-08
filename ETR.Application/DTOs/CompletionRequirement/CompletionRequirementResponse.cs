namespace ETR.Application.DTOs.CompletionRequirement;

public class CompletionRequirementResponse
{
    public int RequirementId { get; set; }
    public int CourseId { get; set; }
    public string RequirementName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsMandatory { get; set; }
    public int DisplayOrder { get; set; }
    public string? RequirementType { get; set; }
    public decimal? ThresholdValue { get; set; }
    public int VersionNo { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
