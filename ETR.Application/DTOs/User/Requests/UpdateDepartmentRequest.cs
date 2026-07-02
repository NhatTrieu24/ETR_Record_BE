namespace ETR.Application.DTOs;

public record UpdateDepartmentRequest(int DepartmentId, string DepartmentName, string? Description);
