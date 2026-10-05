namespace ETR.Application.DTOs;

public record DepartmentResponse(int DepartmentId, string DepartmentName, string? Description, string DepartmentCode = "", bool IsTrainingAudience = true);
