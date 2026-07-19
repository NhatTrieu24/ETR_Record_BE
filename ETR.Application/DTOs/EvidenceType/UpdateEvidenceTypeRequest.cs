using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.EvidenceType;

public class UpdateEvidenceTypeRequest
{
    [Required]
    public string TypeName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
