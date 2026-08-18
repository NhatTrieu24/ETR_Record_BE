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
    [Required, EmailAddress, MaxLength(255)] string Username,
    [Required, MaxLength(100)] string Password,
    [Required] int RoleId,
    [Required] int DepartmentId);

public record UpdateAccountStatusRequest(
    [Required] AccountStatus Status);

public record UpdateAccountRoleRequest(
    [Required] int RoleId);

public record UpdateAccountDepartmentRequest(
    [Required] int DepartmentId);
