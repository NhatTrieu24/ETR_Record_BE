using ETR.Domain.Enums;

namespace ETR.Domain.Entities;

public class UserProfile : BaseEntity
{
    public int AccountId { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? Organization { get; set; }

    /// <summary>Overall learner status (Active/Withdrawn/Graduated) — independent of any single
    /// Enrollment.Status, which tracks a specific class enrollment rather than the person overall.</summary>
    public LearnerStatus Status { get; set; } = LearnerStatus.Active;

    public Account Account { get; set; } = null!;
}
