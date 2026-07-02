namespace ETR.Application.DTOs;

public record UpdateRoleRequest(int RoleId, string RoleName, string? Description);
