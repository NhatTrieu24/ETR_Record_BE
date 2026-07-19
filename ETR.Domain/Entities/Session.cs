namespace ETR.Domain.Entities;

public class Session : BaseEntity
{
    public int SessionId { get; set; }
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
    public string SessionTitle { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public string? Location { get; set; }
    public bool IsConfirmed { get; set; }
    public int? ConfirmedByAccountId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public bool IsAssessmentRequired { get; set; } = false;
    public bool IsChecklistRequired { get; set; } = false;
}
