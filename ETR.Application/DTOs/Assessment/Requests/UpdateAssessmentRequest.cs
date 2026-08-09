using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.Assessment.Requests;

public class UpdateAssessmentRequest
{
    [Required]
    public int AssessmentId { get; set; }

    [Required]
    public int SubjectId { get; set; }

    [Required]
    public string ComponentName { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Theory|Practical)$", ErrorMessage = "AssessmentType must be 'Theory' or 'Practical'")]
    public string AssessmentType { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "Weight must be between 0 and 100")]
    public decimal Weight { get; set; }
    public decimal PassingScore { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}
