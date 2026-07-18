namespace ETR.Domain.Entities;

public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public int? CreatedByAccountId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByAccountId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
