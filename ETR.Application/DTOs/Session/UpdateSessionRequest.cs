using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.Session;

public class UpdateSessionRequest
{
    [Required]
    [MaxLength(200)]
    public string SessionTitle { get; set; } = string.Empty;

    [Required]
    public DateTime SessionDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public bool IsAssessmentRequired { get; set; }
    public bool IsChecklistRequired { get; set; }
    public int? AssessmentId { get; set; }
    public int? PracticalChecklistId { get; set; }

    // Phase 2: Flight / Simulator Training Extensions
    public ETR.Domain.Enums.TrainingType? TrainingType { get; set; }
    public string? LessonCode { get; set; }
}

