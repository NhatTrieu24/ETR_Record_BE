using System.ComponentModel.DataAnnotations;
using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record AccountResponse(
    int AccountId,
    string Username,
    int? RoleId,
    int? DepartmentId,
    AccountStatus Status,
    bool IsActive);

public record CreateAccountRequest(
    [Required, EmailAddress, MaxLength(255), RegularExpression(@"^[a-zA-Z][a-zA-Z0-9._%+-]*@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Tên đăng nhập phải là email hợp lệ, bắt đầu bằng chữ cái, không được bắt đầu bằng số hoặc ký tự đặc biệt không hợp lệ.")] string Username,
    [Required, MinLength(6), MaxLength(100)] string Password,
    [Required] int RoleId,
    [Required] int DepartmentId);

public record UpdateAccountStatusRequest(
    [Required] AccountStatus Status);

public record UpdateAccountRoleRequest(
    [Required] int RoleId);

public record UpdateAccountDepartmentRequest(
    [Required] int DepartmentId);
