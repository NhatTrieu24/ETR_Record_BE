namespace ETR.Application.DTOs;

public record ExportJobResponse(
    int ExportJobId,
    int RequestedBy,
    string ExportType,
    string FileName,
    string FilePath,
    string Status,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    DateTime? DownloadExpiredAt);
