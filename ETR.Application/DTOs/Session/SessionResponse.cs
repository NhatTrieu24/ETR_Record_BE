namespace ETR.Application.DTOs.Session;

public class SessionResponse
{
    public int SessionId { get; set; }
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
    public string SessionTitle { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public string? Location { get; set; }
    
    // Status fields
    public bool IsConfirmed { get; set; }
    public int? ConfirmedByAccountId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public bool IsAssessmentRequired { get; set; }
    public bool IsChecklistRequired { get; set; }
    public int? AssessmentId { get; set; }
    public string? AssessmentName { get; set; }
    public string? AssessmentType { get; set; }
    public int? PracticalChecklistId { get; set; }
    public string? PracticalChecklistName { get; set; }
}
