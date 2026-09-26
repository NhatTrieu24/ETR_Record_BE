using System.ComponentModel.DataAnnotations.Schema;
using ETR.Domain.Enums;

namespace ETR.Domain.Entities;

public class Account : BaseEntity
{
    public int AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int DepartmentId { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Active;

    /// <summary>
    /// Computed status property for backward compatibility with DTOs and API callers.
    /// Not stored as a column in the database; always derived from Status and IsDeleted.
    /// </summary>
    [NotMapped]
    public bool IsActive
    {
        get => Status == AccountStatus.Active && !IsDeleted;
        set => Status = value ? AccountStatus.Active : AccountStatus.Inactive;
    }

    public UserProfile Profile { get; set; } = null!;
}
