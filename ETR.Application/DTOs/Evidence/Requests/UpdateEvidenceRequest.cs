namespace ETR.Application.DTOs;

public record UpdateEvidenceRequest(
    int EvidenceFileId,
    int EvidenceTypeId,
    string FileName,
    string FilePath,
    string? FileExtension,
    string? MimeType,
    long FileSize);
