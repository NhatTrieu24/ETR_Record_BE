namespace ETR.Application.DTOs;

public record UpdateUserRequest(int UserId, string FullName, string Email, int RoleId, int DepartmentId, bool IsActive);
