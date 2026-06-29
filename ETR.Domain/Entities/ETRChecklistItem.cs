namespace ETR.Domain.Entities;

public class ETRChecklistItem : BaseEntity
{
    public int ChecklistItemId { get; set; }
    public int TemplateId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}
