namespace ETR.Domain.Entities;

public class Learner : BaseEntity
{
    public int LearnerId { get; set; }
    public string LearnerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? IdentificationNumber { get; set; }
    public string? Organization { get; set; }
    public string Status { get; set; } = string.Empty;
    public int LearnerTypeId { get; set; }
}
