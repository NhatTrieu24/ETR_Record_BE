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

    public Account Account { get; set; } = null!;
}
