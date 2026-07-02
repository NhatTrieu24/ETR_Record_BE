namespace ETR.Application.DTOs;

public record UpdateChecklistProgressRequest(
    bool IsCompleted,
    int? VerifiedByUserId,
    string? Comment);
