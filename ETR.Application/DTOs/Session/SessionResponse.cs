namespace ETR.Application.DTOs.Session;

public class SessionResponse
{
    public int SessionId { get; set; }
    public int ClassId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SessionTitle { get; set; } = string.Empty;
    public DateTime? SessionDate { get; set; }
    public string? Location { get; set; }
    
    // Status fields
    public bool IsConfirmed { get; set; }
    public int? ConfirmedByAccountId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public bool IsAssessmentRequired { get; set; }
    public bool IsChecklistRequired { get; set; }
    public int? AssessmentId { get; set; }
    public ETR.Application.DTOs.Assessment.Responses.AssessmentResponse? Assessment { get; set; }
    public int? PracticalChecklistId { get; set; }
    public ETR.Application.DTOs.PracticalChecklist.PracticalChecklistResponse? PracticalChecklist { get; set; }

    // Phase 2: Flight / Simulator Training Extensions
    public ETR.Domain.Enums.TrainingType TrainingType { get; set; } = ETR.Domain.Enums.TrainingType.Theory;
    public string? LessonCode { get; set; }

    // Phase 3: Instructor assignment details for session
    public int? InstructorAccountId { get; set; }
    public string? InstructorName { get; set; }

    // Phase 4: Facility & Time Slot Extensions
    public int? FacilityId { get; set; }
    public string? FacilityCode { get; set; }
    public string? FacilityName { get; set; }
    public ETR.Domain.Enums.FacilityType? FacilityType { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public bool IsRemedial { get; set; }
}

