namespace ETR.Domain.Entities;

public class Class : BaseEntity
{
    public int ClassId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Location { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
}
