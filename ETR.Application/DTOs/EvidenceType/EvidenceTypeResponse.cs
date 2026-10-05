namespace ETR.Application.DTOs.EvidenceType;

public class EvidenceTypeResponse
{
    public int EvidenceTypeId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DepartmentScope { get; set; }
    public string? SubjectTypeScope { get; set; }
    public bool IsMandatory { get; set; }
    public string Category { get; set; } = "SubjectEvidence";
}

