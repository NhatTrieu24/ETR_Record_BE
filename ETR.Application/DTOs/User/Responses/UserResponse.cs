namespace ETR.Application.DTOs;

public record UserResponse(int UserId, string Username, string FullName, string Email, int RoleId, int DepartmentId, bool IsActive);
