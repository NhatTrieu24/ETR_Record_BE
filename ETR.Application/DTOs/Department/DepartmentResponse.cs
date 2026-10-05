namespace ETR.Application.DTOs.Department;

public class DepartmentResponse
{
    public int DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsTrainingAudience { get; set; } = true;
}
