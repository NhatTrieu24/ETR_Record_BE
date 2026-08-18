using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record ExportJobResponse(
    int ExportJobId,
    int RequestedByAccountId,
    string ExportType,
    string FileName,
    string FilePath,
    ExportJobStatus Status,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    DateTime? DownloadExpiredAt,
    int? ETRCourseRecordId = null);
