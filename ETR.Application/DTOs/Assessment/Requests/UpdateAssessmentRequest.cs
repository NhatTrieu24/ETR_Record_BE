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
    public string AssessmentType { get; set; } = string.Empty;

    public decimal Weight { get; set; }
    public decimal PassingScore { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}
