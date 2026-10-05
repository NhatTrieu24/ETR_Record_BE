using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.Department;

public class UpdateDepartmentRequest
{
    public string? DepartmentCode { get; set; }

    [Required]
    public string DepartmentName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsTrainingAudience { get; set; } = true;
}
