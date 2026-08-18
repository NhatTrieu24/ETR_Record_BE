using ETR.Domain.Enums;

namespace ETR.Domain.Entities;

public class ExportJob : BaseEntity
{
    public int ExportJobId { get; set; }
    public int RequestedByAccountId { get; set; }
    public string ExportType { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public ExportJobStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? DownloadExpiredAt { get; set; }
    public int? ETRCourseRecordId { get; set; }
}
