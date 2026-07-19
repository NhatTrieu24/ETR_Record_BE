using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.CompletionRequirement;

public class CreateCompletionRequirementRequest
{
    [Required]
    public int CourseId { get; set; }

    [Required]
    public string RequirementName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsMandatory { get; set; }
    
    public int DisplayOrder { get; set; }
}
