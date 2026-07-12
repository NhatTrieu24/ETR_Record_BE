namespace ETR.Domain.Entities;

public class RetakeHistory : BaseEntity
{
    public int RetakeHistoryId { get; set; }
    public int SubjectResultId { get; set; }
    public DateTime RetakeDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal PreviousScore { get; set; }
    public decimal NewScore { get; set; }
    public int AuthorizedBy { get; set; }
    public int AttemptNo { get; set; }
}
