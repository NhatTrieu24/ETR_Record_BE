using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.EvidenceType;

public class CreateEvidenceTypeRequest
{
    [Required]
    [MaxLength(50)]
    public string TypeCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(450)]
    public string TypeName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public string? DepartmentScope { get; set; }
    public string? SubjectTypeScope { get; set; }
    public bool IsMandatory { get; set; } = false;
    public string Category { get; set; } = "SubjectEvidence";
}

