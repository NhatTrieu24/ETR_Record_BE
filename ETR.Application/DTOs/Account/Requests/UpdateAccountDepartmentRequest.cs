using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record UpdateAccountDepartmentRequest(
    [Required] int DepartmentId
);
