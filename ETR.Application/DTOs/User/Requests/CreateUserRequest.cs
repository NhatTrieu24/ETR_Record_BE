namespace ETR.Application.DTOs;

public record CreateUserRequest(string Username, string PasswordHash, string FullName, string Email, int RoleId, int DepartmentId);
