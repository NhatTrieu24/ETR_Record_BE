namespace ETR.Domain.Entities;

public class LearnerType : BaseEntity
{
    public int LearnerTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
