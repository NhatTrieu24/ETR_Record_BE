using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.Department;

public class CreateDepartmentRequest
{
    [Required]
    public string DepartmentName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
