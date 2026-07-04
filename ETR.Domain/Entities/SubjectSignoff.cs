namespace ETR.Domain.Entities;

public class SubjectSignoff : BaseEntity
{
    public int SubjectSignoffId { get; set; }
    public int SubjectResultId { get; set; }
    public int SignoffBy { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime SignoffAt { get; set; }
    public string? Comment { get; set; }
}
