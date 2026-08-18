using ETR.Domain.Enums;

namespace ETR.Domain.Entities;

public class Account : BaseEntity
{
    public int AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int DepartmentId { get; set; }
    public AccountStatus Status { get; set; }
    public bool IsActive { get; set; } = true;

    public UserProfile Profile { get; set; } = null!;
}

