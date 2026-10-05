using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.Session;

public class CreateSessionRequest
{
    [Required]
    public int ClassId { get; set; }

    [Required]
    public int SubjectId { get; set; }

    [Required]
    [MaxLength(200)]
    public string SessionTitle { get; set; } = string.Empty;

    [Required]
    public DateTime SessionDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public bool IsAssessmentRequired { get; set; } = false;
    public bool IsChecklistRequired { get; set; } = false;
    public int? AssessmentId { get; set; }
    public int? PracticalChecklistId { get; set; }

    // Phase 2: Flight / Simulator Training Extensions
    public ETR.Domain.Enums.TrainingType TrainingType { get; set; } = ETR.Domain.Enums.TrainingType.Theory;
    public string? LessonCode { get; set; }

    // Phase 4: Facility & Time Slot Extensions
    public int? FacilityId { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public bool IsRemedial { get; set; } = false;
}

