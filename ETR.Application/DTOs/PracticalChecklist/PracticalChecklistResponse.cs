namespace ETR.Application.DTOs.PracticalChecklist;

public class PracticalChecklistResponse
{
    public int PracticalChecklistId { get; set; }
    public int CourseId { get; set; }
    public int SubjectId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}
