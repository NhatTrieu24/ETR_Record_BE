namespace ETR.Domain.Entities;

public class Account : BaseEntity
{
    public int AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int DepartmentId { get; set; }
    public string Status { get; set; } = string.Empty;

    public UserProfile Profile { get; set; } = null!;
}
