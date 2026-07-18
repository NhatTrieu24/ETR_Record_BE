namespace ETR.Application.DTOs;

public record AccountResponse(
    int AccountId,
    string Username,
    int RoleId,
    int DepartmentId,
    string Status);

public record CreateAccountRequest(
    string Username,
    string Password,
    int RoleId,
    int DepartmentId);

public record UpdateAccountStatusRequest(
    string Status);
