namespace ETR.Domain.Entities;

public class EvidenceType : BaseEntity
{
    public int EvidenceTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
